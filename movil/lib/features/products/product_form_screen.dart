import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../models/catalog.dart';
import '../../models/product.dart';
import '../../services/catalog_service.dart';
import '../../services/product_service.dart';

/// Crear o editar un producto. Si [product] es null, es alta.
class ProductFormScreen extends StatefulWidget {
  const ProductFormScreen({super.key, this.product});
  final Product? product;

  @override
  State<ProductFormScreen> createState() => _ProductFormScreenState();
}

class _ProductFormScreenState extends State<ProductFormScreen> {
  final _formKey = GlobalKey<FormState>();
  late final Product _model;
  bool get _isNew => widget.product == null;

  late final TextEditingController _name;
  late final TextEditingController _code;
  late final TextEditingController _description;
  late final TextEditingController _barCode;
  late final TextEditingController _price;
  late final TextEditingController _stock;
  late final TextEditingController _minReorder;

  List<NamedItem> _categories = [];
  List<NamedItem> _labs = [];
  List<NamedItem> _uoms = [];
  bool _loadingCatalogs = true;
  bool _saving = false;
  String? _catalogError;

  @override
  void initState() {
    super.initState();
    final p = widget.product;
    _model = Product(
      id: p?.id ?? '',
      uomId: p?.uomId ?? '',
      laboratoryId: p?.laboratoryId ?? '',
      categoryId: p?.categoryId ?? '',
      isActive: p?.isActive ?? true,
      availableInPos: p?.availableInPos ?? true,
    );
    _name = TextEditingController(text: p?.productName ?? '');
    _code = TextEditingController(text: p?.productCode ?? '');
    _description = TextEditingController(text: p?.description ?? '');
    _barCode = TextEditingController(text: p?.barCode ?? '');
    _price = TextEditingController(text: p == null ? '' : p.salePrice.toString());
    _stock = TextEditingController(text: p == null ? '0' : p.currentStock.toString());
    _minReorder =
        TextEditingController(text: p == null ? '0' : p.minReorderQuantity.toString());
    _loadCatalogs();
  }

  @override
  void dispose() {
    for (final c in [_name, _code, _description, _barCode, _price, _stock, _minReorder]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _loadCatalogs() async {
    setState(() {
      _loadingCatalogs = true;
      _catalogError = null;
    });
    try {
      final svc = context.read<CatalogService>();
      final results = await Future.wait([
        svc.categories(),
        svc.laboratories(),
        svc.unitsOfMeasurement(),
      ]);
      setState(() {
        _categories = results[0];
        _labs = results[1];
        _uoms = results[2];
      });
    } on ApiException catch (e) {
      setState(() => _catalogError = e.message);
    } finally {
      if (mounted) setState(() => _loadingCatalogs = false);
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_model.uomId.isEmpty) {
      _snack('Selecciona una unidad de medida.');
      return;
    }
    if (_model.laboratoryId.isEmpty) {
      _snack('Selecciona un laboratorio.');
      return;
    }

    _model.productName = _name.text.trim();
    _model.productCode = _code.text.trim();
    _model.description = _description.text.trim();
    _model.barCode = _barCode.text.trim();
    _model.salePrice = double.tryParse(_price.text.trim()) ?? 0;
    _model.currentStock = int.tryParse(_stock.text.trim()) ?? 0;
    _model.minReorderQuantity = int.tryParse(_minReorder.text.trim()) ?? 0;

    setState(() => _saving = true);
    try {
      final svc = context.read<ProductService>();
      if (_isNew) {
        await svc.create(_model);
      } else {
        await svc.update(_model);
      }
      if (!mounted) return;
      _snack(_isNew ? 'Producto creado.' : 'Producto actualizado.');
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _confirmDelete() async {
    final service = context.read<ProductService>();
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Eliminar producto'),
        content: Text('¿Eliminar "${_name.text}"?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Cancelar')),
          FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Eliminar')),
        ],
      ),
    );
    if (ok != true) return;
    setState(() => _saving = true);
    try {
      await service.delete(_model.id);
      if (!mounted) return;
      _snack('Producto eliminado.');
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _snack(String msg) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(msg)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isNew ? 'Nuevo producto' : 'Editar producto'),
        actions: [
          if (!_isNew)
            IconButton(
              tooltip: 'Eliminar',
              onPressed: _saving ? null : _confirmDelete,
              icon: const Icon(Icons.delete_outline),
            ),
        ],
      ),
      body: _loadingCatalogs
          ? const Center(child: CircularProgressIndicator())
          : _catalogError != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(_catalogError!, textAlign: TextAlign.center),
                        const SizedBox(height: 12),
                        OutlinedButton(
                            onPressed: _loadCatalogs,
                            child: const Text('Reintentar')),
                      ],
                    ),
                  ),
                )
              : Form(
                  key: _formKey,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      _field(_name, 'Nombre del producto',
                          validator: (v) => (v == null || v.trim().length < 5)
                              ? 'Mínimo 5 caracteres'
                              : null),
                      _field(_description, 'Descripción',
                          maxLines: 2,
                          validator: (v) => (v == null || v.trim().length < 5)
                              ? 'Mínimo 5 caracteres'
                              : null),
                      _field(_code, 'Código (opcional)'),
                      _field(_barCode, 'Código de barras (opcional)'),
                      _field(_price, 'Precio de venta',
                          keyboardType:
                              const TextInputType.numberWithOptions(decimal: true),
                          inputFormatters: [
                            FilteringTextInputFormatter.allow(
                                RegExp(r'[0-9.]'))
                          ],
                          validator: (v) =>
                              (double.tryParse(v ?? '') == null)
                                  ? 'Precio inválido'
                                  : null),
                      Row(
                        children: [
                          Expanded(
                            child: _field(_stock, 'Stock actual',
                                keyboardType: TextInputType.number,
                                inputFormatters: [
                                  FilteringTextInputFormatter.digitsOnly
                                ]),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: _field(_minReorder, 'Stock mínimo',
                                keyboardType: TextInputType.number,
                                inputFormatters: [
                                  FilteringTextInputFormatter.digitsOnly
                                ]),
                          ),
                        ],
                      ),
                      _dropdown(
                        label: 'Categoría',
                        items: _categories,
                        value: _model.categoryId,
                        onChanged: (v) =>
                            setState(() => _model.categoryId = v ?? ''),
                      ),
                      _dropdown(
                        label: 'Laboratorio',
                        items: _labs,
                        value: _model.laboratoryId,
                        onChanged: (v) =>
                            setState(() => _model.laboratoryId = v ?? ''),
                      ),
                      _dropdown(
                        label: 'Unidad de medida',
                        items: _uoms,
                        value: _model.uomId,
                        onChanged: (v) => setState(() => _model.uomId = v ?? ''),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: const Text('Disponible en POS'),
                        value: _model.availableInPos,
                        onChanged: (v) =>
                            setState(() => _model.availableInPos = v),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: const Text('Activo'),
                        value: _model.isActive,
                        onChanged: (v) => setState(() => _model.isActive = v),
                      ),
                      const SizedBox(height: 16),
                      FilledButton.icon(
                        onPressed: _saving ? null : _save,
                        icon: _saving
                            ? const SizedBox(
                                height: 18,
                                width: 18,
                                child:
                                    CircularProgressIndicator(strokeWidth: 2))
                            : const Icon(Icons.save),
                        label: Text(_isNew ? 'Crear producto' : 'Guardar cambios'),
                      ),
                    ],
                  ),
                ),
    );
  }

  Widget _field(
    TextEditingController c,
    String label, {
    int maxLines = 1,
    TextInputType? keyboardType,
    List<TextInputFormatter>? inputFormatters,
    String? Function(String?)? validator,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextFormField(
        controller: c,
        maxLines: maxLines,
        keyboardType: keyboardType,
        inputFormatters: inputFormatters,
        validator: validator,
        decoration: InputDecoration(labelText: label),
      ),
    );
  }

  Widget _dropdown({
    required String label,
    required List<NamedItem> items,
    required String value,
    required ValueChanged<String?> onChanged,
  }) {
    final hasValue = items.any((e) => e.id == value);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: DropdownButtonFormField<String>(
        initialValue: hasValue ? value : null,
        isExpanded: true,
        decoration: InputDecoration(labelText: label),
        items: items
            .map((e) => DropdownMenuItem(value: e.id, child: Text(e.name)))
            .toList(),
        onChanged: onChanged,
      ),
    );
  }
}
