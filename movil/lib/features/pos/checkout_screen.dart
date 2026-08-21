import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../models/catalog.dart';
import '../../models/discount.dart';
import '../../models/sale.dart';
import '../../providers/auth_provider.dart';
import '../../providers/cart_provider.dart';
import '../../services/catalog_service.dart';
import '../../services/discount_service.dart';
import '../../services/sale_service.dart';
import 'pos_dialogs.dart';
import 'sale_completed_screen.dart';

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key, required this.cashSession});
  final CashSession cashSession;

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  List<PaymentMethod> _methods = [];
  List<Discount> _discounts = [];
  PosSettings _settings =
      PosSettings(maxCashierDiscountPct: 15, maxCashierDiscountAmount: 50);

  // Cliente
  final _customerCtrl = TextEditingController();
  Timer? _customerDebounce;
  List<Customer> _customerResults = [];
  Customer? _customer;
  bool _searchingCustomer = false;

  // Autorización de supervisor (descuentos sobre el límite)
  String _supervisorToken = '';

  bool _loading = true;
  bool _saving = false;
  String? _error;

  bool get _isCashier => context.read<AuthProvider>().rolName == 'Cajero';

  @override
  void initState() {
    super.initState();
    _loadCatalogs();
  }

  @override
  void dispose() {
    _customerDebounce?.cancel();
    _customerCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadCatalogs() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final catalog = context.read<CatalogService>();
      final sale = context.read<SaleService>();
      final results = await Future.wait([
        catalog.paymentMethods(),
        context.read<DiscountService>().active(),
        sale.posSettings(),
        catalog.getDefaultCustomer(),
      ]);
      setState(() {
        _methods = results[0] as List<PaymentMethod>;
        _discounts = results[1] as List<Discount>;
        _settings = results[2] as PosSettings;
        // Precarga el cliente genérico del tenant para que cobrar nunca
        // quede bloqueado por falta de cliente. Si por algún motivo no llegó
        // (sin red, tenant sin sembrar), el picker queda en modo búsqueda,
        // como era antes de esto.
        _customer ??= results[3] as Customer?;
      });
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  // ── Cliente ────────────────────────────────────────────────
  void _onCustomerChanged(String value) {
    _customerDebounce?.cancel();
    _customerDebounce = Timer(const Duration(milliseconds: 400), () {
      _searchCustomers(value.trim());
    });
  }

  Future<void> _searchCustomers(String term) async {
    if (term.isEmpty) {
      setState(() => _customerResults = []);
      return;
    }
    setState(() => _searchingCustomer = true);
    try {
      final res = await context.read<CatalogService>().searchCustomers(term);
      if (mounted) setState(() => _customerResults = res);
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _searchingCustomer = false);
    }
  }

  void _selectCustomer(Customer c) {
    setState(() {
      _customer = c;
      _customerResults = [];
      _customerCtrl.clear();
    });
    FocusScope.of(context).unfocus();
  }

  Future<void> _openNewCustomerDialog() async {
    final created = await newCustomerDialog(
      context,
      context.read<CatalogService>(),
      initialFullName: _customerCtrl.text.trim(),
    );
    if (created != null) _selectCustomer(created);
  }

  // ── Descuentos ─────────────────────────────────────────────
  bool _overCashierLimit(DiscountResult r) {
    if (!_isCashier) return false;
    if (r.id.isNotEmpty) return false; // catálogo no requiere autorización
    if (r.type == 'Percentage') {
      return r.value > _settings.maxCashierDiscountPct;
    }
    if (r.type == 'FixedAmount') {
      return r.value > _settings.maxCashierDiscountAmount;
    }
    return false;
  }

  /// Pide autorización si corresponde. Devuelve true si se puede aplicar.
  Future<bool> _authorizeIfNeeded(DiscountResult r) async {
    if (!_overCashierLimit(r)) return true;
    final token = await supervisorAuthDialog(
      context,
      context.read<SaleService>(),
      reason:
          'El descuento manual supera el límite para cajeros (${_settings.maxCashierDiscountPct.toStringAsFixed(0)}% o ${currency(_settings.maxCashierDiscountAmount)}). Autoriza con un supervisor.',
    );
    if (token == null) return false;
    _supervisorToken = token;
    return true;
  }

  Future<void> _editLineDiscount(SaleLine line) async {
    final cart = context.read<CartProvider>();
    final result = await pickDiscount(
      context,
      baseAmount: line.lineSubtotal,
      catalog: _discounts,
      title: 'Descuento por línea',
    );
    if (result == null) return;
    if (!await _authorizeIfNeeded(result)) return;
    cart.setLineDiscount(line,
        type: result.type,
        value: result.value,
        id: result.id,
        label: result.label);
  }

  Future<void> _editHeaderDiscount() async {
    final cart = context.read<CartProvider>();
    final result = await pickDiscount(
      context,
      baseAmount: cart.headerBase,
      catalog: _discounts,
      title: 'Descuento global',
    );
    if (result == null) return;
    if (!await _authorizeIfNeeded(result)) return;
    cart.setHeaderDiscount(
        type: result.type,
        value: result.value,
        id: result.id,
        label: result.label);
  }

  // ── Cobro ──────────────────────────────────────────────────
  Future<void> _charge() async {
    final cart = context.read<CartProvider>();
    if (cart.isEmpty) return;
    if (_customer == null) {
      _snack('Selecciona un cliente antes de cobrar.');
      return;
    }
    final payments = await _paymentSheet(cart.total);
    if (payments == null || payments.isEmpty) return;
    await _finalize(payments);
  }

  Future<List<SalePayment>?> _paymentSheet(double total) {
    final lines = <SalePayment>[];
    PaymentMethod? method = _methods.isNotEmpty ? _methods.first : null;
    final amountCtrl = TextEditingController();

    return showModalBottomSheet<List<SalePayment>>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(sheetContext).viewInsets.bottom,
        ),
        child: StatefulBuilder(
          builder: (context, setSheet) {
            final paid = lines.fold<double>(0, (s, l) => s + l.amountGiven);
            final pending = (total - paid).clamp(0, double.infinity).toDouble();
            final change = (paid - total).clamp(0, double.infinity).toDouble();

            void addLine() {
              final amount = double.tryParse(amountCtrl.text.trim()) ?? 0;
              if (method == null || amount <= 0) return;
              final returned = method!.requiresChanges
                  ? (paid + amount - total).clamp(0, double.infinity).toDouble()
                  : 0.0;
              setSheet(() {
                lines.add(SalePayment(
                  paymentMethodId: method!.id,
                  paymentMethodName: method!.name,
                  iconCss: method!.iconCss,
                  amountGiven: amount,
                  amountReturned: returned,
                ));
                amountCtrl.clear();
              });
            }

            return SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text('Cobrar venta',
                      style:
                          TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Theme.of(context)
                          .colorScheme
                          .primary
                          .withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('Total a cobrar'),
                        Text(currency(total),
                            style: const TextStyle(
                                fontSize: 18, fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<String>(
                    initialValue: method?.id,
                    isExpanded: true,
                    decoration:
                        const InputDecoration(labelText: 'Método de pago'),
                    items: _methods
                        .map((m) =>
                            DropdownMenuItem(value: m.id, child: Text(m.name)))
                        .toList(),
                    onChanged: (v) => setSheet(
                        () => method = _methods.firstWhere((m) => m.id == v)),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: amountCtrl,
                          keyboardType: const TextInputType.numberWithOptions(
                              decimal: true),
                          decoration: InputDecoration(
                            labelText: 'Monto (Bs.)',
                            hintText: pending > 0
                                ? 'Pendiente: ${currency(pending)}'
                                : null,
                          ),
                          onSubmitted: (_) => addLine(),
                        ),
                      ),
                      const SizedBox(width: 8),
                      FilledButton.tonal(
                        onPressed: addLine,
                        child: const Text('Agregar'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  for (var i = 0; i < lines.length; i++)
                    ListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      title: Text(lines[i].paymentMethodName),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(currency(lines[i].amountGiven)),
                          IconButton(
                            icon: const Icon(Icons.close, size: 18),
                            onPressed: () => setSheet(() => lines.removeAt(i)),
                          ),
                        ],
                      ),
                    ),
                  const Divider(),
                  _kv('Total pagado', currency(paid)),
                  if (pending > 0)
                    _kv('Pendiente', currency(pending), color: Colors.red),
                  if (change > 0)
                    _kv('Vuelto', currency(change), color: Colors.green),
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: paid + 0.0001 < total
                        ? null
                        : () => Navigator.pop(sheetContext, lines),
                    icon: const Icon(Icons.check),
                    label: const Text('Confirmar venta'),
                  ),
                  const SizedBox(height: 12),
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  /// Descarta la venta y vuelve al POS: una pantalla de cobro sin carrito no
  /// tiene nada que hacer.
  Future<void> _discardSale() async {
    final cart = context.read<CartProvider>();
    if (cart.isEmpty) return;
    if (!await confirmDiscardSale(context, cart)) return;
    cart.clear();
    if (mounted) Navigator.pop(context);
  }

  Future<void> _finalize(List<SalePayment> payments) async {
    final cart = context.read<CartProvider>();
    setState(() => _saving = true);
    try {
      final req = SaleRequest(
        customerId: _customer!.id,
        cashSessionId: widget.cashSession.id,
        detail: cart.lines.toList(),
        payments: payments,
        headerDiscountId: cart.headerDiscountId,
        headerDiscountType: cart.headerDiscountType,
        headerDiscountValue: cart.headerDiscountValue,
        headerDiscountAmount: cart.headerDiscountAmount,
        supervisorAuthToken: _supervisorToken,
      );

      // Capturar datos para el recibo antes de limpiar.
      final total = cart.total;
      final paid = payments.fold<double>(0, (s, p) => s + p.amountGiven);
      final change = (paid - total).clamp(0, double.infinity).toDouble();
      final detail = cart.lines.toList();
      final lineDiscounts = cart.totalLineDiscounts;
      final headerDiscount = cart.headerDiscountAmount;
      final customerName = _customer!.fullName;

      await context.read<SaleService>().create(req);
      if (!mounted) return;
      cart.clear();
      await Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (_) => SaleCompletedScreen(
            customerName: customerName,
            total: total,
            change: change,
            payments: payments,
            detail: detail,
            totalLineDiscounts: lineDiscounts,
            headerDiscountAmount: headerDiscount,
          ),
        ),
      );
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _snack(String m) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(m)));
  }

  @override
  Widget build(BuildContext context) {
    final cart = context.watch<CartProvider>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Cobrar'),
        actions: [
          // Es acá donde se suele decidir que la venta no va, así que el mismo
          // acceso que en el POS.
          if (!cart.isEmpty)
            IconButton(
              tooltip: 'Descartar venta',
              icon: const Icon(Icons.remove_shopping_cart_outlined),
              onPressed: _discardSale,
            ),
        ],
      ),
      // El carrito se muestra de inmediato; los catálogos (métodos de pago,
      // descuentos, settings) cargan en segundo plano sin bloquear la pantalla.
      body: Column(
        children: [
          if (_error != null)
            Material(
              color: Theme.of(context).colorScheme.errorContainer,
              child: ListTile(
                dense: true,
                leading: const Icon(Icons.warning_amber, color: Colors.red),
                title: Text(_error!),
                trailing: TextButton(
                    onPressed: _loadCatalogs, child: const Text('Reintentar')),
              ),
            ),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.only(bottom: 16),
              children: [
                _customerSection(),
                const Divider(height: 1),
                ...cart.lines.map(_lineTile),
                if (cart.lines.isNotEmpty) ...[
                  const Divider(),
                  _totalsSection(cart),
                ],
              ],
            ),
          ),
          _bottomBar(cart),
        ],
      ),
    );
  }

  // ── Secciones UI ───────────────────────────────────────────
  Widget _customerSection() {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_customer != null)
            Card(
              child: ListTile(
                leading: const Icon(Icons.person),
                title: Text(_customer!.fullName),
                subtitle: _customer!.documentNumber.isEmpty
                    ? null
                    : Text(_customer!.documentNumber),
                trailing: IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => setState(() => _customer = null),
                ),
              ),
            )
          else ...[
            TextField(
              controller: _customerCtrl,
              onChanged: _onCustomerChanged,
              decoration: InputDecoration(
                labelText: 'Buscar cliente',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _searchingCustomer
                    ? const Padding(
                        padding: EdgeInsets.all(12),
                        child: SizedBox(
                            height: 16,
                            width: 16,
                            child: CircularProgressIndicator(strokeWidth: 2)),
                      )
                    : null,
              ),
            ),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton.icon(
                onPressed: _openNewCustomerDialog,
                icon: const Icon(Icons.person_add_alt_1, size: 18),
                label: const Text('Nuevo cliente'),
              ),
            ),
            for (final c in _customerResults)
              ListTile(
                dense: true,
                title: Text(c.fullName),
                subtitle:
                    c.documentNumber.isEmpty ? null : Text(c.documentNumber),
                onTap: () => _selectCustomer(c),
              ),
          ],
        ],
      ),
    );
  }

  Widget _lineTile(SaleLine line) {
    final cart = context.read<CartProvider>();
    return Dismissible(
      key: ValueKey(line.product.id),
      direction: DismissDirection.endToStart,
      background: Container(
        color: Colors.red,
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 20),
        child: const Icon(Icons.delete, color: Colors.white),
      ),
      onDismissed: (_) => cart.remove(line),
      child: ListTile(
        title: Text(line.product.productName,
            maxLines: 1, overflow: TextOverflow.ellipsis),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${currency(line.unitPrice)} c/u'),
            if (line.hasDiscount)
              Row(
                children: [
                  Flexible(
                    child: Text(
                      '${line.discountLabel}  − ${currency(line.lineTotalDiscounts)}',
                      style: const TextStyle(color: Colors.green, fontSize: 12),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  InkWell(
                    onTap: () => cart.clearLineDiscount(line),
                    child: const Padding(
                      padding: EdgeInsets.all(2),
                      child: Icon(Icons.close, size: 14, color: Colors.red),
                    ),
                  ),
                ],
              ),
          ],
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            IconButton(
              visualDensity: VisualDensity.compact,
              icon: Icon(
                  line.hasDiscount ? Icons.percent : Icons.percent_outlined,
                  color: line.hasDiscount ? Colors.green : null,
                  size: 20),
              onPressed: () => _editLineDiscount(line),
            ),
            IconButton(
              visualDensity: VisualDensity.compact,
              icon: const Icon(Icons.remove_circle_outline),
              onPressed: () => cart.decrement(line),
            ),
            Text('${line.quantity}',
                style: const TextStyle(fontWeight: FontWeight.bold)),
            IconButton(
              visualDensity: VisualDensity.compact,
              icon: const Icon(Icons.add_circle_outline),
              onPressed: () => cart.increment(line),
            ),
            SizedBox(
              width: 70,
              child: Text(currency(line.lineTotal),
                  textAlign: TextAlign.end,
                  style: const TextStyle(fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _totalsSection(CartProvider cart) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Column(
        children: [
          _kv('Subtotal', currency(cart.subtotal)),
          if (cart.totalLineDiscounts > 0)
            _kv('Desc. por línea', '− ${currency(cart.totalLineDiscounts)}',
                color: Colors.green),
          if (cart.hasHeaderDiscount)
            Row(
              children: [
                Expanded(
                  child: Text(cart.headerDiscountLabel.isEmpty
                      ? 'Descuento global'
                      : cart.headerDiscountLabel),
                ),
                Text('− ${currency(cart.headerDiscountAmount)}',
                    style: const TextStyle(color: Colors.green)),
                InkWell(
                  onTap: cart.clearHeaderDiscount,
                  child: const Padding(
                    padding: EdgeInsets.only(left: 6),
                    child: Icon(Icons.close, size: 16, color: Colors.red),
                  ),
                ),
              ],
            ),
          const SizedBox(height: 4),
          OutlinedButton.icon(
            onPressed: _editHeaderDiscount,
            icon: const Icon(Icons.local_offer_outlined, size: 18),
            label: Text(cart.hasHeaderDiscount
                ? 'Cambiar descuento global'
                : 'Agregar descuento global'),
          ),
        ],
      ),
    );
  }

  Widget _bottomBar(CartProvider cart) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Total', style: TextStyle(fontSize: 18)),
                Text(currency(cart.total),
                    style: const TextStyle(
                        fontSize: 22, fontWeight: FontWeight.bold)),
              ],
            ),
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: (_saving || _loading || cart.isEmpty) ? null : _charge,
              icon: (_saving || _loading)
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.point_of_sale),
              label: Text(_loading
                  ? 'Cargando opciones…'
                  : _customer == null
                      ? 'Selecciona un cliente'
                      : 'Cobrar ${currency(cart.total)}'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _kv(String label, String value, {Color? color}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label),
            Text(value, style: TextStyle(color: color)),
          ],
        ),
      );
}
