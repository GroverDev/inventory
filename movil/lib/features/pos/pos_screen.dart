import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../core/utils/media_url.dart';
import '../../models/cash_session.dart';
import '../../models/catalog.dart';
import '../../models/discount.dart';
import '../../models/held_sale.dart';
import '../../models/product.dart';
import '../../providers/auth_provider.dart';
import '../../providers/cart_provider.dart';
import '../../services/catalog_service.dart';
import '../../services/discount_service.dart';
import '../../services/held_sale_service.dart';
import '../../services/product_service.dart';
import '../../services/sale_service.dart';
import 'cart_undo.dart';
import 'cash_close_screen.dart';
import 'checkout_screen.dart';
import 'held_sale_resume.dart';
import 'held_sales_screen.dart';
import 'pos_dialogs.dart';
import 'product_info_sheet.dart';
import 'serial_picker_sheet.dart';

class PosScreen extends StatefulWidget {
  const PosScreen({super.key});

  @override
  State<PosScreen> createState() => _PosScreenState();
}

class _PosScreenState extends State<PosScreen> {
  final _searchCtrl = TextEditingController();

  CashSession? _session;
  bool _checkingSession = true;
  String? _sessionError;

  List<Product> _allProducts = [];
  List<Discount> _discounts = [];
  List<PaymentMethod> _paymentMethods = [];
  PosSettings _settings =
      PosSettings(maxCashierDiscountPct: 15, maxCashierDiscountAmount: 50);
  Customer? _defaultCustomer;
  bool _loadingCatalogs = true;
  String _search = '';
  String _category = '';

  /// Cuántas ventas hay en espera en esta sucursal, para el badge y para
  /// avisar antes de cerrar caja. Se recarga tras cada acción que las toque.
  int _heldCount = 0;

  @override
  void initState() {
    super.initState();
    _checkSession();
    _loadCatalogs();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _checkSession() async {
    setState(() {
      _checkingSession = true;
      _sessionError = null;
    });
    try {
      _session = await context.read<SaleService>().activeSession();
    } on ApiException catch (e) {
      _sessionError = e.message;
    } finally {
      if (mounted) setState(() => _checkingSession = false);
    }
  }

  /// Todo lo que el POS necesita para operar, en un solo viaje — igual que la
  /// web en `PointOfSaleView.vue` (`onMounted`). Cada pieza se resuelve por su
  /// cuenta: si una falla (sin red, por ejemplo) las demás igual quedan
  /// disponibles, en vez de dejar toda la pantalla sin catálogos por un solo
  /// endpoint caído.
  Future<void> _loadCatalogs() async {
    setState(() => _loadingCatalogs = true);
    final productService = context.read<ProductService>();
    final discountService = context.read<DiscountService>();
    final catalogService = context.read<CatalogService>();
    final saleService = context.read<SaleService>();
    final heldSaleService = context.read<HeldSaleService>();

    final results = await Future.wait<Object?>([
      productService.getAll().catchError((_) => <Product>[]),
      discountService.active().catchError((_) => <Discount>[]),
      catalogService.paymentMethods().catchError((_) => <PaymentMethod>[]),
      saleService.posSettings().catchError((_) =>
          PosSettings(maxCashierDiscountPct: 15, maxCashierDiscountAmount: 50)),
      catalogService.getDefaultCustomer().catchError((_) => null),
      heldSaleService.getHeld().catchError((_) => <HeldSale>[]),
    ]);

    if (!mounted) return;
    setState(() {
      _allProducts =
          (results[0] as List<Product>).where((p) => p.isActive).toList();
      _discounts = results[1] as List<Discount>;
      _paymentMethods = results[2] as List<PaymentMethod>;
      _settings = results[3] as PosSettings;
      _defaultCustomer = results[4] as Customer?;
      _heldCount = (results[5] as List<HeldSale>).length;
      _loadingCatalogs = false;
    });
  }

  Future<void> _refreshHeldCount() async {
    try {
      final items = await context.read<HeldSaleService>().getHeld();
      if (mounted) setState(() => _heldCount = items.length);
    } on ApiException {
      // Silencioso: el badge es informativo, no crítico para vender.
    }
  }

  List<String> get _categories {
    final set = <String>{};
    for (final p in _allProducts) {
      if (p.categoryName.trim().isNotEmpty) set.add(p.categoryName);
    }
    final list = set.toList()..sort();
    return list;
  }

  List<Product> get _filtered {
    return _allProducts.where((p) {
      if (_search.isNotEmpty &&
          !p.productName.toLowerCase().contains(_search.toLowerCase())) {
        return false;
      }
      if (_category.isNotEmpty && p.categoryName != _category) return false;
      return true;
    }).toList();
  }

  void _snack(String m) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(m)));
  }

  Future<void> _openSession() async {
    final amount = await openCashDialog(context);
    if (amount == null) return;
    try {
      await context.read<SaleService>().openSession(amount);
      await _checkSession();
    } on ApiException catch (e) {
      _snack(e.message);
    }
  }

  /// Descarta la venta en curso. Vive en la barra superior, lejos del botón de
  /// cobrar: es destructiva y no debe quedar a un dedo de distancia de la
  /// acción que confirma.
  Future<void> _discardSale() async {
    final cart = context.read<CartProvider>();
    if (cart.isEmpty) return;
    if (!await confirmDiscardSale(context, cart)) return;
    cart.clear();
    _snack('Venta descartada.');
  }

  /// Deja la venta en curso guardada en el servidor para retomarla después
  /// (`api/HeldSale`), y limpia el carrito para atender a otro cliente.
  Future<void> _holdCurrentSale() async {
    final cart = context.read<CartProvider>();
    if (cart.isEmpty) return;
    final note = await holdSaleDialog(context);
    if (note == null) return;
    try {
      final payload = buildHeldPayload(cart);
      await context.read<HeldSaleService>().hold(
            payload,
            label: note,
            customerId: cart.customer?.id,
            itemsCount: cart.itemCount,
            total: cart.total,
          );
      cart.clear();
      _snack('Venta puesta en espera.');
      _refreshHeldCount();
    } on ApiException catch (e) {
      _snack(e.message);
    }
  }

  Future<void> _openHeldList() async {
    final taken = await Navigator.push<HeldSale>(
      context,
      MaterialPageRoute(builder: (_) => const HeldSalesScreen()),
    );
    _refreshHeldCount();
    if (taken != null && taken.payload != null) _resumeHeldSale(taken);
  }

  /// Resuelve el payload guardado contra el catálogo, precios y descuentos
  /// vigentes, y lo vuelca en el carrito. Ver `held_sale_resume.dart`.
  void _resumeHeldSale(HeldSale h) {
    final cart = context.read<CartProvider>();
    final isCashier = context.read<AuthProvider>().rolName == 'Cajero';
    try {
      final payload =
          HeldPayload.fromJson(jsonDecode(h.payload!) as Map<String, dynamic>);
      final resumed = resolveHeldSale(
        payload,
        catalog: _allProducts,
        discounts: _discounts,
        settings: _settings,
        isCashier: isCashier,
      );
      applyResumedSale(cart, resumed);
      if (resumed.warnings.isNotEmpty && mounted) {
        showDialog<void>(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Venta retomada'),
            content: Text(resumed.warnings.join('\n\n')),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Entendido')),
            ],
          ),
        );
      }
    } catch (_) {
      _snack('No se pudo leer la venta en espera.');
    }
  }

  Future<void> _closeSession() async {
    if (_session == null) return;
    final cart = context.read<CartProvider>();

    // Una venta en espera no es un faltante: no se cobró. Pero conviene
    // resolverla antes de cerrar para que no quede olvidada — igual que en
    // la web (alerta en el modal de cerrar caja).
    if (_heldCount > 0) {
      final continuar = await confirm(
        context,
        title: 'Ventas en espera',
        message: 'Hay $_heldCount venta(s) en espera en esta sucursal. '
            'No son un faltante — no se cobraron — pero conviene resolverlas '
            'antes de cerrar.',
        confirmLabel: 'Cerrar de todas formas',
        cancelLabel: 'Revisarlas',
      );
      if (!continuar) {
        await _openHeldList();
        return;
      }
    }

    final closed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => CashCloseScreen(
          session: _session!,
          paymentMethods: _paymentMethods,
        ),
      ),
    );
    if (closed != true) return;
    if (mounted) setState(() => _session = null);
    // Sin caja abierta la venta no se puede cobrar: el carrito no sobrevive
    // al turno.
    cart.clear();
    _snack('Caja cerrada correctamente.');
  }

  Future<void> _addMovement() async {
    if (_session == null) return;
    final result = await movementDialog(context);
    if (result == null) return;
    try {
      await context.read<SaleService>().addMovement(
            _session!.id,
            movementType: result.type,
            amount: result.amount,
            description: result.description,
          );
      await _checkSession();
      _snack('Movimiento registrado.');
    } on ApiException catch (e) {
      _snack(e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();

    if (_checkingSession) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }

    if (_session == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Punto de venta')),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.lock_clock, size: 56, color: Colors.grey),
                const SizedBox(height: 12),
                Text(
                  _sessionError ?? 'No tienes una caja abierta.',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 16),
                FilledButton.icon(
                  onPressed: _openSession,
                  icon: const Icon(Icons.point_of_sale),
                  label: const Text('Abrir caja'),
                ),
                TextButton(
                    onPressed: _checkSession,
                    child: const Text('Reintentar')),
              ],
            ),
          ),
        ),
      );
    }

    final categories = _categories;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Punto de venta'),
        actions: [
          // Solo existen cuando hay algo que hacer con la venta en curso: un
          // botón permanentemente deshabilitado enseña a ignorar esa zona.
          if (!cart.isEmpty)
            IconButton(
              tooltip: 'Poner en espera',
              icon: const Icon(Icons.pause_circle_outlined),
              onPressed: _holdCurrentSale,
            ),
          if (!cart.isEmpty)
            IconButton(
              tooltip: 'Descartar venta',
              icon: const Icon(Icons.remove_shopping_cart_outlined),
              onPressed: _discardSale,
            ),
          IconButton(
            tooltip: 'Ventas en espera',
            icon: Badge(
              label: Text('$_heldCount'),
              isLabelVisible: _heldCount > 0,
              child: const Icon(Icons.hourglass_top_outlined),
            ),
            onPressed: _openHeldList,
          ),
          Center(
            child: Padding(
              padding: const EdgeInsets.only(right: 8),
              child: Chip(
                visualDensity: VisualDensity.compact,
                avatar: const Icon(Icons.point_of_sale, size: 16),
                label: Text(currency(_session!.openingAmount)),
              ),
            ),
          ),
          PopupMenuButton<String>(
            onSelected: (v) {
              if (v == 'movement') _addMovement();
              if (v == 'close') _closeSession();
            },
            itemBuilder: (_) => const [
              PopupMenuItem(
                value: 'movement',
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.receipt_long),
                  title: Text('Registrar movimiento'),
                ),
              ),
              PopupMenuItem(
                value: 'close',
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(Icons.lock),
                  title: Text('Cerrar caja'),
                ),
              ),
            ],
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(56),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 8),
            child: TextField(
              controller: _searchCtrl,
              onChanged: (v) => setState(() => _search = v.trim()),
              style: const TextStyle(color: Colors.white),
              decoration: InputDecoration(
                hintText: 'Buscar producto...',
                hintStyle: const TextStyle(color: Colors.white70),
                prefixIcon: const Icon(Icons.search, color: Colors.white70),
                filled: true,
                fillColor: Colors.white24,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide.none,
                ),
              ),
            ),
          ),
        ),
      ),
      body: _loadingCatalogs
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                if (categories.isNotEmpty)
                  SizedBox(
                    height: 48,
                    child: ListView(
                      scrollDirection: Axis.horizontal,
                      padding: const EdgeInsets.symmetric(horizontal: 12),
                      children: [
                        Padding(
                          padding: const EdgeInsets.only(right: 6, top: 8),
                          child: ChoiceChip(
                            label: const Text('Todos'),
                            selected: _category == '',
                            onSelected: (_) => setState(() => _category = ''),
                          ),
                        ),
                        for (final c in categories)
                          Padding(
                            padding: const EdgeInsets.only(right: 6, top: 8),
                            child: ChoiceChip(
                              label: Text(c),
                              selected: _category == c,
                              onSelected: (_) => setState(
                                  () => _category = _category == c ? '' : c),
                            ),
                          ),
                      ],
                    ),
                  ),
                Expanded(
                  child: _filtered.isEmpty
                      ? const Center(child: Text('Sin productos disponibles.'))
                      : GridView.builder(
                          padding: const EdgeInsets.all(12),
                          gridDelegate:
                              const SliverGridDelegateWithMaxCrossAxisExtent(
                            maxCrossAxisExtent: 200,
                            childAspectRatio: 0.85,
                            crossAxisSpacing: 10,
                            mainAxisSpacing: 10,
                          ),
                          itemCount: _filtered.length,
                          itemBuilder: (context, i) => _ProductTile(
                            product: _filtered[i],
                            catalog: _allProducts,
                          ),
                        ),
                ),
              ],
            ),
      bottomNavigationBar: cart.isEmpty
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: FilledButton(
                  onPressed: () async {
                    await Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => CheckoutScreen(
                          cashSession: _session!,
                          paymentMethods: _paymentMethods,
                          discounts: _discounts,
                          settings: _settings,
                          defaultCustomer: _defaultCustomer,
                        ),
                      ),
                    );
                    // Al volver, refrescamos la caja (la venta cambió totales)
                    // y el conteo de ventas en espera (pudo poner una).
                    if (mounted) {
                      _checkSession();
                      _refreshHeldCount();
                    }
                  },
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      CircleAvatar(
                        radius: 14,
                        backgroundColor: Colors.white24,
                        child: Text('${cart.itemCount}',
                            style: const TextStyle(
                                color: Colors.white, fontSize: 13)),
                      ),
                      const Text('Ver carrito'),
                      Text(currency(cart.total),
                          style:
                              const TextStyle(fontWeight: FontWeight.bold)),
                    ],
                  ),
                ),
              ),
            ),
    );
  }
}

class _ProductTile extends StatelessWidget {
  const _ProductTile({required this.product, required this.catalog});
  final Product product;

  /// Catálogo completo ya cargado del POS: la ficha lo usa para resolver una
  /// alternativa a un [Product] completo sin otro viaje al servidor.
  final List<Product> catalog;

  /// Agregar un producto pasa por acá siempre: uno serializado abre el
  /// selector de series en vez de sumar de una (mismo criterio que
  /// `addToCart` en `PointOfSaleView.vue`).
  void _addToCart(BuildContext context, CartProvider cart) {
    if (product.usesSerial) {
      showSerialPicker(context, product);
    } else {
      cart.add(product);
    }
  }

  void _decrement(BuildContext context, CartProvider cart) {
    if (product.usesSerial) {
      showSerialPicker(context, product);
      return;
    }
    final wasLast = cart.quantityOf(product.id) <= 1;
    cart.decrementByProduct(product.id);
    if (wasLast) showUndoSnack(context, cart);
  }

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    final qty = cart.quantityOf(product.id);
    final outOfStock = product.currentStock <= 0;
    final hasImage = mediaUrl(product.imagePath, thumb: true).isNotEmpty;
    return InkWell(
      onTap: outOfStock ? null : () => _addToCart(context, cart),
      borderRadius: BorderRadius.circular(12),
      child: Card(
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: qty > 0
              ? BorderSide(
                  color: Theme.of(context).colorScheme.primary, width: 1.5)
              : BorderSide.none,
        ),
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    Center(
                      child: hasImage
                          ? ClipRRect(
                              borderRadius: BorderRadius.circular(8),
                              child: Image.network(
                                mediaUrl(product.imagePath, thumb: true),
                                fit: BoxFit.cover,
                                width: double.infinity,
                                errorBuilder: (_, __, ___) => Icon(
                                    Icons.medication_outlined,
                                    size: 40,
                                    color: Theme.of(context).colorScheme.primary),
                              ),
                            )
                          : Icon(Icons.medication_outlined,
                              size: 40, color: Theme.of(context).colorScheme.primary),
                    ),
                    if (qty > 0)
                      Positioned(
                        right: 0,
                        top: 0,
                        child: CircleAvatar(
                          radius: 11,
                          backgroundColor: Theme.of(context).colorScheme.primary,
                          child: Text('$qty',
                              style: const TextStyle(color: Colors.white, fontSize: 11)),
                        ),
                      ),
                    // La ficha se abre desde acá: es la pregunta del mostrador
                    // ("¿para qué sirve?", "¿hay algo más barato?") y no
                    // debería obligar a salir del punto de venta.
                    Positioned(
                      left: -4,
                      top: -4,
                      child: IconButton(
                        tooltip: 'Ver composición, prospecto y alternativas',
                        visualDensity: VisualDensity.compact,
                        icon: Icon(Icons.info_outline,
                            size: 18, color: Theme.of(context).colorScheme.primary),
                        onPressed: () => showProductInfoSheet(
                          context,
                          product: product,
                          catalog: catalog,
                          onAdd: (p) => p.usesSerial
                              ? showSerialPicker(context, p)
                              : cart.add(p),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Text(product.productName,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                      fontWeight: FontWeight.w600, fontSize: 13)),
              const SizedBox(height: 4),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(currency(product.salePrice),
                      style: const TextStyle(fontWeight: FontWeight.bold)),
                  Text(
                    outOfStock ? 'Sin stock' : 'x${product.currentStock}',
                    style: TextStyle(
                        fontSize: 11,
                        color: outOfStock ? Colors.red : Colors.grey),
                  ),
                ],
              ),
              if (qty == 0)
                SizedBox(
                  width: double.infinity,
                  child: OutlinedButton(
                    onPressed: outOfStock ? null : () => _addToCart(context, cart),
                    style: OutlinedButton.styleFrom(
                        padding: EdgeInsets.zero,
                        visualDensity: VisualDensity.compact),
                    child: Text(outOfStock ? 'Sin stock' : 'Agregar',
                        style: const TextStyle(fontSize: 12)),
                  ),
                )
              else
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    IconButton(
                      visualDensity: VisualDensity.compact,
                      icon: const Icon(Icons.remove_circle_outline),
                      onPressed: () => _decrement(context, cart),
                    ),
                    Text('$qty',
                        style: const TextStyle(fontWeight: FontWeight.bold)),
                    IconButton(
                      visualDensity: VisualDensity.compact,
                      icon: const Icon(Icons.add_circle_outline),
                      onPressed: () => _addToCart(context, cart),
                    ),
                  ],
                ),
            ],
          ),
        ),
      ),
    );
  }
}
