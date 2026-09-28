import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../models/held_sale.dart';
import '../../providers/cart_provider.dart';
import '../../services/held_sale_service.dart';

/// Ventas en espera de la sucursal activa: las deja cualquier caja y las
/// puede retomar cualquier caja (`api/HeldSale`, acotado por la sucursal
/// activa en el servidor — ver `HeldSaleRepository`).
///
/// Al retomar una, esta pantalla se cierra devolviendo el [HeldSale] ya
/// tomado del servidor (con su `payload`); quien la abrió se encarga de
/// resolverlo contra el catálogo vigente ([resolveHeldSale] en
/// `held_sale_resume.dart`).
class HeldSalesScreen extends StatefulWidget {
  const HeldSalesScreen({super.key});

  @override
  State<HeldSalesScreen> createState() => _HeldSalesScreenState();
}

class _HeldSalesScreenState extends State<HeldSalesScreen> {
  static final _timeFmt = DateFormat('HH:mm');

  List<HeldSale> _items = const [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final items = await context.read<HeldSaleService>().getHeld();
      if (mounted) setState(() => _items = items);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// Solo se puede retomar con el carrito actual vacío: si el cajero tiene una
  /// venta armada, primero tiene que terminarla o ponerla en espera. El chequeo
  /// va acá (y no antes de abrir la pantalla) porque es al tocar "Retomar"
  /// cuando la venta se saca de la espera en el servidor — antes de eso no hay
  /// nada que deshacer.
  Future<void> _resume(HeldSale h) async {
    final cart = context.read<CartProvider>();
    if (!cart.isEmpty) {
      _snack('Termine o ponga en espera la venta actual antes de retomar otra.');
      return;
    }
    try {
      final taken = await context.read<HeldSaleService>().take(h.id);
      if (mounted) Navigator.pop(context, taken);
    } on ApiException catch (e) {
      _snack(e.message);
      _load(); // otra caja se pudo haber adelantado: refrescar la lista
    }
  }

  Future<void> _discard(HeldSale h) async {
    final ok = await confirm(
      context,
      title: 'Descartar venta en espera',
      message: '¿Descartar${h.label.isEmpty ? '' : ' «${h.label}»'} por '
          '${currency(h.total)}? No se puede recuperar.',
      confirmLabel: 'Descartar',
      destructive: true,
    );
    if (!ok) return;
    try {
      await context.read<HeldSaleService>().discard(h.id);
      _load();
    } on ApiException catch (e) {
      _snack(e.message);
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
    return Scaffold(
      appBar: AppBar(title: const Text('Ventas en espera')),
      body: _body(),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 48, color: Colors.grey),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(_error!, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Reintentar')),
          ],
        ),
      );
    }
    if (_items.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text('No hay ventas en espera en esta sucursal.',
              textAlign: TextAlign.center, style: TextStyle(color: Colors.grey)),
        ),
      );
    }
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        padding: const EdgeInsets.symmetric(vertical: 8),
        itemCount: _items.length,
        itemBuilder: (context, i) => _card(_items[i]),
      ),
    );
  }

  Widget _card(HeldSale h) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(h.label.isEmpty ? '(sin nota)' : h.label,
                          style: const TextStyle(fontWeight: FontWeight.bold)),
                      if (h.customerName.isNotEmpty)
                        Text(h.customerName,
                            style: const TextStyle(fontSize: 12, color: Colors.grey)),
                      Text('${_timeFmt.format(h.created)} · dejó ${h.userName}',
                          style: const TextStyle(fontSize: 11, color: Colors.grey)),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(currency(h.total),
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                    Text('${h.itemsCount} ítem(s)',
                        style: const TextStyle(fontSize: 11, color: Colors.grey)),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton.icon(
                  onPressed: () => _discard(h),
                  icon: const Icon(Icons.delete_outline, size: 18, color: Colors.red),
                  label: const Text('Descartar', style: TextStyle(color: Colors.red)),
                ),
                const SizedBox(width: 8),
                FilledButton.icon(
                  onPressed: () => _resume(h),
                  icon: const Icon(Icons.play_arrow, size: 18),
                  label: const Text('Retomar'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
