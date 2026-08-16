import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../models/access_menu.dart';
import '../../models/catalog.dart';
import '../../models/product.dart';
import '../../providers/auth_provider.dart';
import '../../services/catalog_service.dart';
import '../../services/product_service.dart';

/// Crear, editar o consultar un producto. Si [product] es null, es alta.
///
/// El modo consulta (solo lectura) se decide con los permisos granulares del
/// formulario `products-admin`: sin `create`/`update` el formulario se muestra
/// deshabilitado y sin botón de guardar.
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

  /// Permiso para grabar: `create` en alta, `update` en edición.
  bool get _canSave => context
      .read<AuthProvider>()
      .can(kProductsForm, _isNew ? PermAction.create : PermAction.update);

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
    if (!_canSave) {
      _snack('No tienes permiso para modificar productos.');
      return;
    }
    if (!_formKey.currentState!.validate()) return;
    if (_model.uomId.isEmpty) {
      _snack('Selecciona una unidad de medida.');
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
    if (!context.read<AuthProvider>().can(kProductsForm, PermAction.delete)) {
      _snack('No tienes permiso para eliminar productos.');
      return;
    }
    final service = context.read<ProductService>();
    final ok = await confirm(
      context,
      title: 'Eliminar producto',
      message: '¿Eliminar "${_name.text}"? Esta acción no se puede deshacer.',
      confirmLabel: 'Eliminar',
      destructive: true,
    );
    if (!ok || !mounted) return;
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
    final auth = context.watch<AuthProvider>();
    final canSave = auth.can(
        kProductsForm, _isNew ? PermAction.create : PermAction.update);
    final canDelete = !_isNew && auth.can(kProductsForm, PermAction.delete);
    final readOnly = !canSave;

    return Scaffold(
      appBar: AppBar(
        title: Text(_isNew
            ? 'Nuevo producto'
            : readOnly
                ? 'Detalle del producto'
                : 'Editar producto'),
        actions: [
          if (canDelete)
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
                      if (readOnly) const _ReadOnlyBanner(),
                      _field(_name, 'Nombre del producto',
                          enabled: !readOnly,
                          validator: (v) => (v == null || v.trim().length < 5)
                              ? 'Mínimo 5 caracteres'
                              : null),
                      _field(_description, 'Descripción',
                          maxLines: 2,
                          enabled: !readOnly,
                          validator: (v) => (v == null || v.trim().length < 5)
                              ? 'Mínimo 5 caracteres'
                              : null),
                      _field(_code, 'Código (opcional)', enabled: !readOnly),
                      _field(_barCode, 'Código de barras (opcional)',
                          enabled: !readOnly),
                      _field(_price, 'Precio de venta',
                          enabled: !readOnly,
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
                                enabled: !readOnly,
                                keyboardType: TextInputType.number,
                                inputFormatters: [
                                  FilteringTextInputFormatter.digitsOnly
                                ]),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: _field(_minReorder, 'Stock mínimo',
                                enabled: !readOnly,
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
                        enabled: !readOnly,
                        onChanged: (v) =>
                            setState(() => _model.categoryId = v ?? ''),
                      ),
                      _dropdown(
                        label: 'Laboratorio',
                        items: _labs,
                        value: _model.laboratoryId,
                        enabled: !readOnly,
                        onChanged: (v) =>
                            setState(() => _model.laboratoryId = v ?? ''),
                      ),
                      _dropdown(
                        label: 'Unidad de medida',
                        items: _uoms,
                        value: _model.uomId,
                        enabled: !readOnly,
                        onChanged: (v) => setState(() => _model.uomId = v ?? ''),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: const Text('Disponible en POS'),
                        value: _model.availableInPos,
                        onChanged: readOnly
                            ? null
                            : (v) => setState(() => _model.availableInPos = v),
                      ),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: const Text('Activo'),
                        value: _model.isActive,
                        onChanged: readOnly
                            ? null
                            : (v) => setState(() => _model.isActive = v),
                      ),
                      const SizedBox(height: 16),
                      if (canSave)
                        FilledButton.icon(
                          onPressed: _saving ? null : _save,
                          icon: _saving
                              ? const SizedBox(
                                  height: 18,
                                  width: 18,
                                  child:
                                      CircularProgressIndicator(strokeWidth: 2))
                              : const Icon(Icons.save),
                          label:
                              Text(_isNew ? 'Crear producto' : 'Guardar cambios'),
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
    bool enabled = true,
    TextInputType? keyboardType,
    List<TextInputFormatter>? inputFormatters,
    String? Function(String?)? validator,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextFormField(
        controller: c,
        enabled: enabled,
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
    bool enabled = true,
  }) {
    final hasValue = items.any((e) => e.id == value);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: DropdownButtonFormField<String>(
        initialValue: hasValue ? value : null,
        isExpanded: true,
        decoration: InputDecoration(labelText: label, enabled: enabled),
        items: items
            .map((e) => DropdownMenuItem(value: e.id, child: Text(e.name)))
            .toList(),
        // Un onChanged nulo deshabilita el control (se ve atenuado).
        onChanged: enabled ? onChanged : null,
      ),
    );
  }
}

/// Aviso de modo consulta para usuarios sin permiso de escritura.
class _ReadOnlyBanner extends StatelessWidget {
  const _ReadOnlyBanner();

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: scheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Icon(Icons.lock_outline, size: 20, color: scheme.onSurfaceVariant),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              'Solo lectura: no tienes permiso para modificar productos.',
              style: TextStyle(color: scheme.onSurfaceVariant, fontSize: 13),
            ),
          ),
        ],
      ),
    );
  }
}
