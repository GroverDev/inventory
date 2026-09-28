import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_theme.dart';
import '../../core/ui/image_lightbox.dart';
import '../../core/utils/media_url.dart';
import '../../models/pharma.dart';
import '../../models/product.dart';
import '../../services/pharma_service.dart';
import '../../services/product_service.dart';

/// Ficha del producto: composición, prospecto y alternativas — la pregunta
/// del mostrador ("¿para qué sirve?", "¿hay algo más barato?"), sin salir
/// del punto de venta. Espejo reducido de `ProductInfoModal.vue`.
///
/// [catalog] es el catálogo ya cargado del POS: resuelve una alternativa a
/// un [Product] completo sin otro viaje al servidor.
Future<void> showProductInfoSheet(
  BuildContext context, {
  required Product product,
  required List<Product> catalog,
  required void Function(Product) onAdd,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => ProductInfoSheet(product: product, catalog: catalog, onAdd: onAdd),
  );
}

class ProductInfoSheet extends StatefulWidget {
  const ProductInfoSheet({
    super.key,
    required this.product,
    required this.catalog,
    required this.onAdd,
  });

  final Product product;
  final List<Product> catalog;
  final void Function(Product) onAdd;

  @override
  State<ProductInfoSheet> createState() => _ProductInfoSheetState();
}

class _ProductInfoSheetState extends State<ProductInfoSheet> {
  ProductPharma _ficha = ProductPharma();
  List<ProductEquivalent> _equivalentes = [];
  String _prospecto = '';
  bool _loading = true;

  // Precio y stock de este momento: la tarjeta los trae de cuando se cargó
  // el catálogo y pudieron cambiar (otra caja vendió, se corrigió el
  // precio). Arranca con los de la tarjeta y se reemplaza al llegar los
  // actuales.
  double? _livePrice;
  int? _liveStock;

  double get _price => _livePrice ?? widget.product.salePrice;
  int get _stock => _liveStock ?? widget.product.currentStock;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final pharma = context.read<PharmaService>();
    final products = context.read<ProductService>();
    final id = widget.product.id;
    try {
      final results = await Future.wait<Object?>([
        pharma.getByProduct(id).catchError((_) => null),
        pharma.getEquivalents(id).catchError((_) => <ProductEquivalent>[]),
        pharma.getLeaflet(id).catchError((_) => ''),
        products.validateSelection(id).catchError((_) => null),
      ]);
      if (!mounted) return;
      setState(() {
        _ficha = (results[0] as ProductPharma?) ?? ProductPharma();
        _equivalentes = results[1] as List<ProductEquivalent>;
        _prospecto = results[2] as String;
        final v = results[3] as ({double salePrice, int currentStock})?;
        _livePrice = v?.salePrice;
        _liveStock = v?.currentStock;
      });
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _addAlternative(ProductEquivalent e) {
    Product? full;
    for (final p in widget.catalog) {
      if (p.id == e.productId) {
        full = p;
        break;
      }
    }
    // Si no está en el catálogo ya cargado (filtrado, inactivo, de otra
    // categoría), se arma con lo que trajo la ficha: alcanza para agregarlo.
    full ??= Product(
        id: e.productId,
        productName: e.productName,
        salePrice: e.salePrice,
        currentStock: e.currentStock);
    widget.onAdd(full);
    Navigator.pop(context);
  }

  void _openLightbox(BuildContext context) {
    final src = mediaUrl(widget.product.imagePath);
    if (src.isEmpty) return;
    showImageLightbox(context, src: src, caption: widget.product.productName);
  }

  @override
  Widget build(BuildContext context) {
    final automaticas = _equivalentes.where((e) => !e.isManual).toList();
    final manuales = _equivalentes.where((e) => e.isManual).toList();
    final activos = _ficha.components.where((c) => c.isActiveIngredient).toList();
    final excipientes = _ficha.components.where((c) => !c.isActiveIngredient).toList();
    final sinDatos =
        !_ficha.hasData && _equivalentes.isEmpty && _prospecto.isEmpty && !_loading;

    return DraggableScrollableSheet(
      initialChildSize: 0.85,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (context, scrollController) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 4, 0),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(widget.product.productName,
                          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                      if (widget.product.laboratoryName.isNotEmpty)
                        Text(widget.product.laboratoryName,
                            style: const TextStyle(fontSize: 12, color: Colors.grey)),
                    ],
                  ),
                ),
                IconButton(
                    icon: const Icon(Icons.close), onPressed: () => Navigator.pop(context)),
              ],
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : ListView(
                    controller: scrollController,
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
                    children: [
                      if (mediaUrl(widget.product.imagePath).isNotEmpty)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: Center(
                            child: Column(
                              children: [
                                InkWell(
                                  borderRadius: BorderRadius.circular(10),
                                  onTap: () => _openLightbox(context),
                                  child: ClipRRect(
                                    borderRadius: BorderRadius.circular(10),
                                    child: Image.network(
                                      mediaUrl(widget.product.imagePath),
                                      height: 160,
                                      fit: BoxFit.contain,
                                      errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 4),
                                const Text('Toca para ampliar',
                                    style: TextStyle(fontSize: 11, color: Colors.grey)),
                              ],
                            ),
                          ),
                        ),
                      Row(
                        children: [
                          Expanded(
                              child: _statBox('Precio', currency(_price),
                                  Theme.of(context).colorScheme.primary)),
                          const SizedBox(width: 8),
                          Expanded(
                            child: _statBox(
                              'Stock disponible',
                              _stock > 0 ? '$_stock' : 'Agotado',
                              _stock > 5
                                  ? Colors.green
                                  : _stock > 0
                                      ? Colors.orange
                                      : Colors.red,
                            ),
                          ),
                        ],
                      ),
                      if (activos.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        const Text('Composición',
                            style: TextStyle(fontSize: 12, color: Colors.grey)),
                        const SizedBox(height: 6),
                        Wrap(
                          spacing: 6,
                          runSpacing: 6,
                          children: [
                            for (final c in activos)
                              Chip(
                                label: Text(c.concentrationValue != null
                                    ? '${c.substanceName} ${c.concentrationValue}${c.concentrationUnit}'
                                    : c.substanceName),
                              ),
                          ],
                        ),
                        if (excipientes.isNotEmpty)
                          Padding(
                            padding: const EdgeInsets.only(top: 6),
                            child: Text(
                                'Excipientes: ${excipientes.map((e) => e.substanceName).join(', ')}',
                                style: const TextStyle(fontSize: 12, color: Colors.grey)),
                          ),
                      ],
                      if (_ficha.formName.isNotEmpty ||
                          _ficha.routeName.isNotEmpty ||
                          _ficha.dosageReference.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            if (_ficha.formName.isNotEmpty) _labelValue('Forma', _ficha.formName),
                            if (_ficha.routeName.isNotEmpty) ...[
                              const SizedBox(width: 24),
                              _labelValue('Vía', _ficha.routeName),
                            ],
                          ],
                        ),
                        if (_ficha.dosageReference.isNotEmpty) ...[
                          const SizedBox(height: 8),
                          const Text('Posología de referencia',
                              style: TextStyle(fontSize: 12, color: Colors.grey)),
                          Text(_ficha.dosageReference),
                          const Text('Según prospecto. La dosis la indica quien receta.',
                              style: TextStyle(fontSize: 11, color: Colors.grey)),
                        ],
                      ],
                      if (automaticas.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        Text('Misma composición (${automaticas.length})',
                            style: const TextStyle(fontSize: 12, color: Colors.grey)),
                        for (final e in automaticas) _equivalentTile(e, warn: false),
                      ],
                      if (manuales.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        Text('Otras opciones sugeridas (${manuales.length})',
                            style: const TextStyle(fontSize: 12, color: Colors.grey)),
                        for (final e in manuales) _equivalentTile(e, warn: true),
                      ],
                      if (_prospecto.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        const Text('Prospecto',
                            style: TextStyle(fontSize: 12, color: Colors.grey)),
                        const SizedBox(height: 4),
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            border: Border.all(
                                color: Theme.of(context).colorScheme.outlineVariant),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Text(_prospecto, style: const TextStyle(fontSize: 13)),
                        ),
                        const Text('Información del prospecto del fabricante.',
                            style: TextStyle(fontSize: 11, color: Colors.grey)),
                      ],
                      if (sinDatos)
                        const Padding(
                          padding: EdgeInsets.symmetric(vertical: 24),
                          child: Center(
                            child: Text(
                              'Este producto no tiene datos farmacéuticos cargados.',
                              style: TextStyle(color: Colors.grey),
                            ),
                          ),
                        ),
                    ],
                  ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: FilledButton.icon(
                onPressed: _stock > 0
                    ? () {
                        widget.onAdd(widget.product);
                        Navigator.pop(context);
                      }
                    : null,
                icon: const Icon(Icons.add_shopping_cart),
                label: Text(_stock > 0 ? 'Agregar al carrito' : 'Sin stock'),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _statBox(String label, String value, Color color) => Container(
        padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 12),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: const TextStyle(fontSize: 11, color: Colors.grey)),
            Text(value,
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: color)),
          ],
        ),
      );

  Widget _labelValue(String label, String value) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(fontSize: 11, color: Colors.grey)),
          Text(value),
        ],
      );

  /// Negativo = la alternativa es más barata.
  double _diferencia(ProductEquivalent e) => e.salePrice - _price;

  Widget _equivalentTile(ProductEquivalent e, {required bool warn}) {
    final diff = _diferencia(e);
    return Container(
      margin: const EdgeInsets.only(top: 6),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        border: Border.all(
            color: warn ? Colors.orange.shade200 : Theme.of(context).colorScheme.outlineVariant),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(e.productName,
                    style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
                Text(
                    '${e.reason.isNotEmpty ? '${e.reason} · ' : e.typeLabel.isNotEmpty ? '${e.typeLabel} · ' : ''}stock ${e.currentStock}',
                    style: const TextStyle(fontSize: 11, color: Colors.grey)),
                // Aviso explícito: la composición puede ser distinta, y sin
                // esto un cajero podría entregarla como si fuera lo mismo.
                if (warn)
                  const Text('Sugerencia de la farmacia, puede tener otra composición.',
                      style: TextStyle(fontSize: 10, color: Colors.orange)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(currency(e.salePrice), style: const TextStyle(fontWeight: FontWeight.bold)),
              if (diff < 0)
                Text('${currency(diff.abs())} menos',
                    style: const TextStyle(fontSize: 11, color: Colors.green))
              else if (diff > 0)
                Text('${currency(diff)} más',
                    style: const TextStyle(fontSize: 11, color: Colors.grey)),
              const SizedBox(height: 4),
              SizedBox(
                height: 28,
                child: OutlinedButton(
                  onPressed: e.currentStock > 0 ? () => _addAlternative(e) : null,
                  style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 10),
                      visualDensity: VisualDensity.compact),
                  child: Text(e.currentStock > 0 ? 'Agregar' : 'Sin stock',
                      style: const TextStyle(fontSize: 11)),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
