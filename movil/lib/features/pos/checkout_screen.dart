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
import '../../services/held_sale_service.dart';
import '../../services/sale_service.dart';
import 'cart_undo.dart';
import 'held_sale_resume.dart';
import 'payment_calc.dart';
import 'pos_dialogs.dart';
import 'sale_completed_screen.dart';
import 'serial_picker_sheet.dart';

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({
    super.key,
    required this.cashSession,
    required this.paymentMethods,
    required this.discounts,
    required this.settings,
    this.defaultCustomer,
  });

  final CashSession cashSession;

  // Catálogos que el POS ya cargó al entrar (una sola vez para toda la
  // sesión de venta, igual que `onMounted` en `PointOfSaleView.vue`): acá no
  // se vuelven a pedir.
  final List<PaymentMethod> paymentMethods;
  final List<Discount> discounts;
  final PosSettings settings;
  final Customer? defaultCustomer;

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  // Cliente
  final _customerCtrl = TextEditingController();
  Timer? _customerDebounce;
  List<Customer> _customerResults = [];
  bool _searchingCustomer = false;

  // Autorización de supervisor (descuentos sobre el límite)
  String _supervisorToken = '';

  bool _saving = false;

  bool get _isCashier => context.read<AuthProvider>().rolName == 'Cajero';

  @override
  void initState() {
    super.initState();
    // Precarga el cliente genérico del tenant para que cobrar nunca quede
    // bloqueado por falta de cliente. Si ya había uno elegido (se viene de
    // retomar una venta en espera, por ejemplo), no se pisa.
    final cart = context.read<CartProvider>();
    if (cart.customer == null && widget.defaultCustomer != null) {
      cart.setCustomer(widget.defaultCustomer);
    }
  }

  @override
  void dispose() {
    _customerDebounce?.cancel();
    _customerCtrl.dispose();
    super.dispose();
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
    context.read<CartProvider>().setCustomer(c);
    setState(() => _customerResults = []);
    _customerCtrl.clear();
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
      return r.value > widget.settings.maxCashierDiscountPct;
    }
    if (r.type == 'FixedAmount') {
      return r.value > widget.settings.maxCashierDiscountAmount;
    }
    return false;
  }

  /// Motivo por el que un descuento manual no se puede aplicar aunque lo
  /// autorice un supervisor: supera el tope máximo de la sucursal. '' si no
  /// aplica (los del catálogo no tienen tope aparte). Espejo de
  /// `exceedsMaxDiscount` en `PointOfSaleView.vue`.
  String _exceedsMaxDiscount(DiscountResult r) {
    if (r.id.isNotEmpty) return '';
    final maxPct = widget.settings.maxDiscountPct;
    final maxAmt = widget.settings.maxDiscountAmount;
    if (r.type == 'Percentage' && maxPct != null && r.value > maxPct) {
      return 'El descuento supera el tope máximo del ${maxPct.toStringAsFixed(0)}% de esta sucursal.';
    }
    if (r.type == 'FixedAmount' && maxAmt != null && r.value > maxAmt) {
      return 'El descuento supera el tope máximo de ${currency(maxAmt)} de esta sucursal.';
    }
    return '';
  }

  /// Pide autorización si corresponde. Devuelve true si se puede aplicar.
  Future<bool> _authorizeIfNeeded(DiscountResult r) async {
    if (!_overCashierLimit(r)) return true;
    final token = await supervisorAuthDialog(
      context,
      context.read<SaleService>(),
      reason:
          'El descuento manual supera el límite para cajeros (${widget.settings.maxCashierDiscountPct.toStringAsFixed(0)}% o ${currency(widget.settings.maxCashierDiscountAmount)}). Autoriza con un supervisor.',
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
      catalog: widget.discounts,
      title: 'Descuento por línea',
    );
    if (result == null) return;
    final tope = _exceedsMaxDiscount(result);
    if (tope.isNotEmpty) {
      _snack(tope);
      return;
    }
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
      catalog: widget.discounts,
      title: 'Descuento global',
    );
    if (result == null) return;
    final tope = _exceedsMaxDiscount(result);
    if (tope.isNotEmpty) {
      _snack(tope);
      return;
    }
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
    if (cart.customer == null) {
      _snack('Selecciona un cliente antes de cobrar.');
      return;
    }
    final payments = await _paymentSheet(cart.total);
    if (payments == null || payments.isEmpty) return;
    await _finalize(payments);
  }

  /// El medio de pago que da vuelto (el efectivo). Es el que se preselecciona
  /// al abrir el cobro y tras cada pago mientras falte saldo: en un pago
  /// mixto, lo típico es que el resto se cierre en efectivo.
  PaymentMethod? _cashMethod() {
    for (final m in widget.paymentMethods) {
      if (m.requiresChanges) return m;
    }
    return null;
  }

  String _fmt(double v) => v.toStringAsFixed(2);

  Future<List<SalePayment>?> _paymentSheet(double total) {
    final lines = <SalePayment>[];
    final amountCtrl = TextEditingController();
    // El efectivo es el cobro más común: se deja elegido para escribir
    // directo, igual que `selectCashIfPending` en la web.
    PaymentMethod? method =
        _cashMethod() ?? (widget.paymentMethods.isNotEmpty ? widget.paymentMethods.first : null);

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
            final totalChangeSoFar =
                lines.fold<double>(0, (s, l) => s + l.amountReturned);
            final pending = pendingAmount(total: total, paid: paid);
            final typedAmount = double.tryParse(amountCtrl.text.trim()) ?? 0;
            final givesChange = method?.requiresChanges ?? false;
            final calc = PaymentCalc(
                pending: pending, typedAmount: typedAmount, givesChange: givesChange);
            final exceedsNoChange = method != null && calc.exceedsNoChange;
            final canAdd = method != null && calc.canAdd;

            void selectMethod(PaymentMethod m) {
              setSheet(() {
                method = m;
                // Efectivo: vacío, para escribir lo recibido y ver el vuelto.
                // Tarjeta o QR: el saldo, porque se cobra exacto.
                amountCtrl.text = m.requiresChanges ? '' : _fmt(pending);
              });
            }

            void setAmount(double v) => setSheet(() => amountCtrl.text = _fmt(v));

            void addLine() {
              if (!canAdd) return;
              final m = method!;
              setSheet(() {
                lines.add(SalePayment(
                  paymentMethodId: m.id,
                  paymentMethodName: m.name,
                  iconCss: m.iconCss,
                  amountGiven: typedAmount,
                  amountReturned: calc.liveChange,
                ));
                amountCtrl.clear();
                // Pago mixto: si queda saldo (pagó una parte con tarjeta), el
                // resto normalmente va en efectivo.
                final newPaid = lines.fold<double>(0, (s, l) => s + l.amountGiven);
                final newPending = pendingAmount(total: total, paid: newPaid);
                final cash = _cashMethod();
                if (cash != null && newPending > 0) {
                  method = cash;
                } else {
                  method = null;
                }
              });
            }

            final quicks = givesChange ? quickAmounts(pending) : const <double>[];
            final panel = payPanel(
              exceedsNoChange: exceedsNoChange,
              pending: pending,
              typedAmount: typedAmount,
              liveShortfall: calc.liveShortfall,
              liveChange: calc.liveChange,
              totalChangeSoFar: totalChangeSoFar,
              methodName: method?.name ?? '',
            );

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
                  const Text('Método de pago',
                      style: TextStyle(fontSize: 12, color: Colors.grey)),
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      for (final m in widget.paymentMethods)
                        ChoiceChip(
                          label: Text(m.name),
                          selected: method?.id == m.id,
                          onSelected: (_) => selectMethod(m),
                        ),
                    ],
                  ),
                  if (method != null) ...[
                    const SizedBox(height: 12),
                    TextField(
                      controller: amountCtrl,
                      autofocus: true,
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      onChanged: (_) => setSheet(() {}),
                      onSubmitted: (_) {
                        addLine();
                        final newPaid =
                            lines.fold<double>(0, (s, l) => s + l.amountGiven);
                        if (newPaid + 0.0001 >= total) {
                          Navigator.pop(sheetContext, lines);
                        }
                      },
                      decoration: InputDecoration(
                        labelText: givesChange ? 'Efectivo recibido' : 'Monto a cobrar',
                        hintText: givesChange ? 'Monto recibido' : 'Monto',
                        errorText: exceedsNoChange
                            ? 'No puede superar ${currency(pending)}'
                            : null,
                        suffixIcon: IconButton(
                          icon: const Icon(Icons.add_circle),
                          onPressed: canAdd ? addLine : null,
                        ),
                      ),
                    ),
                    if (givesChange) ...[
                      const SizedBox(height: 8),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          OutlinedButton(
                              onPressed: () => setAmount(pending),
                              child: const Text('Exacto')),
                          for (final v in quicks)
                            OutlinedButton(
                                onPressed: () => setAmount(v),
                                child: Text(currency(v))),
                        ],
                      ),
                    ],
                  ],
                  const SizedBox(height: 12),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    decoration: BoxDecoration(
                      color: (panel.color ?? Theme.of(context).colorScheme.outline)
                          .withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Column(
                      children: [
                        Text(panel.label,
                            style: TextStyle(fontSize: 12, color: panel.color)),
                        if (panel.amount != null)
                          Text(currency(panel.amount!),
                              style: TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: panel.color))
                        else
                          Text(panel.note,
                              style: TextStyle(
                                  fontWeight: FontWeight.w600, color: panel.color)),
                      ],
                    ),
                  ),
                  if (lines.isNotEmpty) ...[
                    const Text('Pagos registrados',
                        style: TextStyle(fontSize: 12, color: Colors.grey)),
                    for (var i = 0; i < lines.length; i++)
                      ListTile(
                        dense: true,
                        contentPadding: EdgeInsets.zero,
                        title: Text(lines[i].paymentMethodName),
                        subtitle: lines[i].amountReturned > 0
                            ? Text('Vuelto ${currency(lines[i].amountReturned)}')
                            : null,
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
                  ],
                  const SizedBox(height: 8),
                  FilledButton.icon(
                    onPressed: pending > 0.0001
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

  /// Deja la venta en espera y vuelve al POS.
  Future<void> _holdSale() async {
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
      if (mounted) Navigator.pop(context);
    } on ApiException catch (e) {
      _snack(e.message);
    }
  }

  Future<void> _finalize(List<SalePayment> payments) async {
    final cart = context.read<CartProvider>();
    setState(() => _saving = true);
    try {
      final req = SaleRequest(
        customerId: cart.customer!.id,
        cashSessionId: widget.cashSession.id,
        detail: cart.lines.toList(),
        payments: payments,
        headerDiscountId: cart.headerDiscountId,
        headerDiscountType: cart.headerDiscountType,
        headerDiscountValue: cart.headerDiscountValue,
        headerDiscountAmount: cart.headerDiscountAmount,
        supervisorAuthToken: _supervisorToken,
      );

      // Capturar datos para el recibo antes de limpiar. El vuelto sale solo
      // de los pagos que lo admiten (efectivo): sumar todo lo pagado menos el
      // total mostraría vuelto aunque se hubiera cobrado de más con tarjeta,
      // que no se devuelve.
      final total = cart.total;
      final change = payments.fold<double>(0, (s, p) => s + p.amountReturned);
      final detail = cart.lines.toList();
      final lineDiscounts = cart.totalLineDiscounts;
      final headerDiscount = cart.headerDiscountAmount;
      final customerName = cart.customer!.fullName;

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
      // Vender de más no es un error: falta la firma de un supervisor. La
      // venta no se grabó, así que se reintenta con el mismo carrito y los
      // mismos pagos en cuanto se obtiene el token.
      if (e.id == kRequiresSupervisorStock) {
        final token = await supervisorAuthDialog(
          context,
          context.read<SaleService>(),
          reason: e.message,
        );
        if (token != null) {
          _supervisorToken = token;
          if (mounted) await _finalize(payments);
          return;
        }
      } else {
        _snack(e.message);
      }
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
          if (!cart.isEmpty)
            IconButton(
              tooltip: 'Poner en espera',
              icon: const Icon(Icons.pause_circle_outlined),
              onPressed: _holdSale,
            ),
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
      body: Column(
        children: [
          Expanded(
            child: ListView(
              padding: const EdgeInsets.only(bottom: 16),
              children: [
                _customerSection(cart),
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
  Widget _customerSection(CartProvider cart) {
    final customer = cart.customer;
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (customer != null)
            Card(
              child: ListTile(
                leading: const Icon(Icons.person),
                title: Text(customer.fullName),
                subtitle: customer.documentNumber.isEmpty
                    ? null
                    : Text(customer.documentNumber),
                trailing: IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => cart.setCustomer(null),
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

  /// Una línea serializada no se ajusta con +/-: hay que decir qué unidad
  /// entra o sale, así que reabre el selector en vez de sumar o restar.
  /// Espejo de `esLineaSerializada`/`reabrirSelector` en
  /// `PointOfSaleView.vue`.
  void _adjustLineQty(SaleLine line, {required bool increase}) {
    final cart = context.read<CartProvider>();
    if (line.serialNumbers.isNotEmpty) {
      showSerialPicker(context, line.product);
      return;
    }
    if (increase) {
      cart.increment(line);
      return;
    }
    final wasLast = line.quantity <= 1;
    cart.decrement(line);
    if (wasLast) showUndoSnack(context, cart);
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
      onDismissed: (_) {
        cart.remove(line);
        showUndoSnack(context, cart);
      },
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
              onPressed: () => _adjustLineQty(line, increase: false),
            ),
            Text('${line.quantity}',
                style: const TextStyle(fontWeight: FontWeight.bold)),
            IconButton(
              visualDensity: VisualDensity.compact,
              icon: const Icon(Icons.add_circle_outline),
              onPressed: () => _adjustLineQty(line, increase: true),
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
              onPressed: (_saving || cart.isEmpty) ? null : _charge,
              icon: _saving
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2))
                  : const Icon(Icons.point_of_sale),
              label: Text(cart.customer == null
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
