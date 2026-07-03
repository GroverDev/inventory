import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../models/product.dart';
import '../../providers/cart_provider.dart';
import '../../services/product_service.dart';
import '../../services/sale_service.dart';
import 'checkout_screen.dart';
import 'pos_dialogs.dart';

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
  bool _loadingProducts = true;
  String _search = '';
  String _category = '';

  @override
  void initState() {
    super.initState();
    _checkSession();
    _loadProducts();
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

  Future<void> _loadProducts() async {
    setState(() => _loadingProducts = true);
    try {
      // Igual que la web: cargamos TODO el catálogo de una vez (GET api/Product)
      // y filtramos en memoria. Solo productos activos; los sin stock se
      // muestran pero no se pueden agregar.
      final all = await context.read<ProductService>().getAll();
      _allProducts = all.where((p) => p.isActive).toList();
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _loadingProducts = false);
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

  Future<void> _closeSession() async {
    if (_session == null) return;
    final result = await closeCashDialog(context, _session!);
    if (result == null) return;
    try {
      await context.read<SaleService>().closeSession(
            _session!.id,
            declaredAmount: result.declaredAmount,
            notes: result.notes,
          );
      if (mounted) setState(() => _session = null);
      _snack('Caja cerrada correctamente.');
    } on ApiException catch (e) {
      _snack(e.message);
    }
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
      body: _loadingProducts
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
                          itemBuilder: (context, i) =>
                              _ProductTile(product: _filtered[i]),
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
                        builder: (_) =>
                            CheckoutScreen(cashSession: _session!),
                      ),
                    );
                    // Al volver, refrescamos la caja (la venta cambió totales).
                    if (mounted) _checkSession();
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
  const _ProductTile({required this.product});
  final Product product;

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    final qty = cart.quantityOf(product.id);
    final outOfStock = product.currentStock <= 0;
    return InkWell(
      onTap: outOfStock ? null : () => cart.add(product),
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
                child: Center(
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      Icon(Icons.medication_outlined,
                          size: 40,
                          color: Theme.of(context).colorScheme.primary),
                      if (qty > 0)
                        Positioned(
                          right: 0,
                          top: 0,
                          child: CircleAvatar(
                            radius: 11,
                            backgroundColor:
                                Theme.of(context).colorScheme.primary,
                            child: Text('$qty',
                                style: const TextStyle(
                                    color: Colors.white, fontSize: 11)),
                          ),
                        ),
                    ],
                  ),
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
                    onPressed: outOfStock ? null : () => cart.add(product),
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
                      onPressed: () => cart.decrementByProduct(product.id),
                    ),
                    Text('$qty',
                        style: const TextStyle(fontWeight: FontWeight.bold)),
                    IconButton(
                      visualDensity: VisualDensity.compact,
                      icon: const Icon(Icons.add_circle_outline),
                      onPressed: () => cart.add(product),
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
