import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../models/product.dart';
import '../../models/stock_serial.dart';
import '../../providers/cart_provider.dart';
import '../../services/stock_movement_service.dart';

/// Selector de números de serie para un producto serializado
/// (`TrackingMode == 'serial'`). Un producto así no se agrega tocando la
/// tarjeta: la cantidad la determina cuántas unidades se eligieron acá.
/// Espejo del selector de series de `PointOfSaleView.vue`.
Future<void> showSerialPicker(BuildContext context, Product product) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => SerialPickerSheet(product: product),
  );
}

class SerialPickerSheet extends StatefulWidget {
  const SerialPickerSheet({super.key, required this.product});
  final Product product;

  @override
  State<SerialPickerSheet> createState() => _SerialPickerSheetState();
}

class _SerialPickerSheetState extends State<SerialPickerSheet> {
  List<StockSerial> _available = [];
  bool _loading = true;
  String? _error;
  late Set<String> _selected;

  @override
  void initState() {
    super.initState();
    final cart = context.read<CartProvider>();
    final existing =
        cart.lines.where((l) => l.product.id == widget.product.id);
    _selected = existing.isEmpty ? <String>{} : {...existing.first.serialNumbers};
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final list = await context
          .read<StockMovementService>()
          .availableSerials(widget.product.id);
      if (mounted) setState(() => _available = list);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _toggle(String serie) {
    setState(() {
      if (!_selected.remove(serie)) _selected.add(serie);
    });
  }

  void _confirm() {
    context.read<CartProvider>().setSerializedLine(widget.product, _selected.toList());
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: MediaQuery.of(context).size.height * 0.75,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 4),
            child: Text(widget.product.productName,
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          ),
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              'Elige qué unidad(es) entregar: la garantía queda atada a ese número.',
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
          ),
          const Divider(height: 16),
          Expanded(child: _body()),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: FilledButton(
                onPressed: _confirm,
                child: Text(_selected.isEmpty
                    ? 'Quitar del carrito'
                    : 'Confirmar ${_selected.length} unidad(es)'),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(_error!, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 8),
            OutlinedButton(onPressed: _load, child: const Text('Reintentar')),
          ],
        ),
      );
    }
    if (_available.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text('No hay unidades disponibles.', textAlign: TextAlign.center),
        ),
      );
    }
    return ListView(
      children: [
        for (final s in _available)
          CheckboxListTile(
            value: _selected.contains(s.serialNumber),
            onChanged: (_) => _toggle(s.serialNumber),
            title: Text(s.serialNumber),
            subtitle: s.expiryDate == null ? null : Text('Vence ${_fmtDate(s.expiryDate!)}'),
          ),
      ],
    );
  }

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
}
