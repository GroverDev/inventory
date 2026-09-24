import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../models/held_sale.dart';
import '../../models/product.dart';
import '../../providers/auth_provider.dart';
import '../../providers/cart_provider.dart';
import '../../services/discount_service.dart';
import '../../services/sale_service.dart';

/// Ventas en espera: el cliente va a buscar dinero y el cajero atiende a otro.
///
/// Se guardan en el servidor, por sucursal, con el mismo formato que la web:
/// cualquier caja, web o móvil, las retoma. No reservan stock ni precios: al
/// retomar se usa el precio de hoy y se quitan los descuentos que ya no valen.

void _snack(BuildContext context, String m) => ScaffoldMessenger.of(context)
  ..hideCurrentSnackBar()
  ..showSnackBar(SnackBar(content: Text(m)));

/// Pone el carrito en espera y lo vacía. Devuelve true si quedó guardada.
Future<bool> holdCurrentSale(BuildContext context) async {
  final cart = context.read<CartProvider>();
  if (cart.isEmpty) return false;

  final labelCtrl = TextEditingController();
  final ok = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: const Text('Poner en espera'),
      content: TextField(
        controller: labelCtrl,
        autofocus: true,
        maxLength: 100,
        decoration: const InputDecoration(
          labelText: 'Nota para reconocerla (opcional)',
          hintText: 'Ej.: señora de blusa roja',
        ),
        onSubmitted: (_) => Navigator.pop(ctx, true),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
        FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Poner en espera')),
      ],
    ),
  );
  // Sin dispose: el diálogo todavía lo usa durante la animación de cierre.
  final label = labelCtrl.text.trim();
  if (ok != true || !context.mounted) return false;

  try {
    await context.read<SaleService>().holdSale(
          label: label,
          customerId: cart.customer?.id,
          itemsCount: cart.itemCount,
          total: cart.total,
          payload: HeldSalePayload.build(
            lines: cart.lines,
            customer: cart.customer,
            header: cart.headerDiscountType.isEmpty
                ? null
                : (
                    id: cart.headerDiscountId,
                    label: cart.headerDiscountLabel,
                    type: cart.headerDiscountType,
                    value: cart.headerDiscountValue,
                  ),
          ),
        );
  } on ApiException catch (e) {
    if (context.mounted) _snack(context, e.message);
    return false;
  }
  cart.clear();
  if (context.mounted) _snack(context, 'Venta puesta en espera.');
  return true;
}

/// Lista las ventas en espera de la sucursal para retomar o descartar.
/// [products] son los del POS, para rearmar el carrito con el precio de hoy.
Future<void> showHeldSalesSheet(BuildContext context, {required List<Product> products}) async {
  await showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => _HeldSalesSheet(products: products, parent: context),
  );
}

class _HeldSalesSheet extends StatefulWidget {
  const _HeldSalesSheet({required this.products, required this.parent});
  final List<Product> products;

  /// Contexto del POS: los avisos se muestran ahí, con la hoja ya cerrada.
  final BuildContext parent;

  @override
  State<_HeldSalesSheet> createState() => _HeldSalesSheetState();
}

class _HeldSalesSheetState extends State<_HeldSalesSheet> {
  static final _hm = DateFormat('HH:mm');
  List<HeldSale>? _items;
  String? _error;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final items = await context.read<SaleService>().heldSales();
      if (mounted) setState(() => _items = items);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    }
  }

  Future<void> _resume(HeldSale h) async {
    final cart = context.read<CartProvider>();
    if (!cart.isEmpty) {
      _snack(context, 'Termine o ponga en espera la venta actual antes de retomar otra.');
      return;
    }
    final sales = context.read<SaleService>();
    final discounts = context.read<DiscountService>();
    final isCashier = context.read<AuthProvider>().rolName == 'Cajero';

    setState(() => _busy = true);
    try {
      // Lo vigente, para decidir qué descuentos siguen valiendo.
      final (activos, topes) = await (discounts.active(), sales.posSettings()).wait;
      final tomada = await sales.takeHeldSale(h.id);

      // Ya salió de la espera: si no se puede retomar, se devuelve tal cual
      // para no perderla (y alguien decide si descartarla).
      Future<Never> devolver(String motivo) async {
        await sales.holdSale(
          label: tomada.label,
          itemsCount: tomada.itemsCount,
          total: tomada.total,
          payload: tomada.payload,
        );
        throw ApiException(motivo);
      }

      RestoredCart r;
      try {
        r = HeldSalePayload.restore(
          tomada.payload,
          products: {for (final p in widget.products) p.id: p},
          activeDiscountIds: {for (final d in activos) d.id},
          settings: topes,
          isCashier: isCashier,
        );
      } on FormatException catch (e) {
        await devolver(e.message);
      }

      if (r.lines.isEmpty) {
        await devolver('Ningún producto de esa venta sigue disponible; quedó en espera. ${r.notices.join(' ')}');
      }
      cart.restore(
        lines: r.lines,
        customer: r.customer,
        headerId: r.header?.id ?? '',
        headerLabel: r.header?.label ?? '',
        headerType: r.header?.type ?? '',
        headerValue: r.header?.value ?? 0,
      );
      if (!mounted) return;
      Navigator.pop(context);
      if (r.notices.isNotEmpty && widget.parent.mounted) {
        await showDialog<void>(
          context: widget.parent,
          builder: (ctx) => AlertDialog(
            title: const Text('Venta retomada con cambios'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [for (final n in r.notices) Padding(padding: const EdgeInsets.only(bottom: 6), child: Text('• $n'))],
            ),
            actions: [FilledButton(onPressed: () => Navigator.pop(ctx), child: const Text('Entendido'))],
          ),
        );
      }
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() => _busy = false);
      _snack(context, e.message);
      _load();   // otra caja pudo haberla retomado antes
    }
  }

  Future<void> _discard(HeldSale h) async {
    final ok = await confirm(
      context,
      title: 'Descartar venta en espera',
      message: '¿Descartar ${h.label.isNotEmpty ? '«${h.label}» ' : ''}por ${currency(h.total)}? '
          'No se puede recuperar.',
      confirmLabel: 'Descartar',
      destructive: true,
    );
    if (!ok || !mounted) return;
    try {
      await context.read<SaleService>().discardHeldSale(h.id);
    } on ApiException catch (e) {
      if (mounted) _snack(context, e.message);
    }
    _load();
  }

  @override
  Widget build(BuildContext context) {
    final items = _items;
    return SafeArea(
      child: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: MediaQuery.of(context).size.height * 0.75),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Padding(
              padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Text('Ventas en espera', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
            ),
            if (_busy) const LinearProgressIndicator(),
            if (_error != null)
              Padding(padding: const EdgeInsets.all(16), child: Text(_error!))
            else if (items == null)
              const Padding(padding: EdgeInsets.all(24), child: Center(child: CircularProgressIndicator()))
            else if (items.isEmpty)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Text('No hay ventas en espera en esta sucursal.', textAlign: TextAlign.center),
              )
            else
              Flexible(
                child: ListView(
                  shrinkWrap: true,
                  children: [
                    for (final h in items)
                      ListTile(
                        enabled: !_busy,
                        title: Text(h.label.isNotEmpty ? h.label : (h.customerName.isNotEmpty ? h.customerName : 'Sin nota')),
                        subtitle: Text([
                          _hm.format(h.created),
                          '${h.itemsCount} producto(s)',
                          if (h.label.isNotEmpty && h.customerName.isNotEmpty) h.customerName,
                          if (h.userName.isNotEmpty) h.userName,
                        ].join(' · ')),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(currency(h.total), style: const TextStyle(fontWeight: FontWeight.bold)),
                            IconButton(
                              tooltip: 'Descartar',
                              icon: const Icon(Icons.delete_outline),
                              onPressed: _busy ? null : () => _discard(h),
                            ),
                          ],
                        ),
                        onTap: () => _resume(h),
                      ),
                  ],
                ),
              ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
  }
}
