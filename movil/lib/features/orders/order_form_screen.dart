import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/catalog.dart';
import '../../models/product.dart';
import '../../models/purchase.dart';
import '../../services/catalog_service.dart';
import '../../services/product_service.dart';
import '../../services/purchase_service.dart';

/// Crear un nuevo pedido (compra a proveedor).
class OrderFormScreen extends StatefulWidget {
  const OrderFormScreen({super.key});

  @override
  State<OrderFormScreen> createState() => _OrderFormScreenState();
}

class _OrderFormScreenState extends State<OrderFormScreen> {
  /// Un pedido nuevo siempre nace en Solicitado; el servidor lo impone al
  /// insertar, sin importar lo que se le mande.
  static const int _requestedStatusId = PurchaseStatusIds.requested;

  List<NamedItem> _providers = [];
  List<PurchaseStatus> _statuses = [];
  NamedItem? _provider;
  PurchaseStatus? _status;
  DateTime _estimatedDate = DateTime.now().add(const Duration(days: 7));
  final List<PurchaseLine> _lines = [];

  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadCatalogs();
  }

  Future<void> _loadCatalogs() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final svc = context.read<CatalogService>();
      final results =
          await Future.wait([svc.providers(), svc.purchaseStatuses()]);
      setState(() {
        _providers = results[0] as List<NamedItem>;
        _statuses = results[1] as List<PurchaseStatus>;
        _status =
            _statuses.where((s) => s.id == _requestedStatusId).firstOrNull;
      });
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  double get _total => _lines.fold(0, (s, l) => s + l.orderFinalPrice);

  Future<void> _addProduct() async {
    final product = await showModalBottomSheet<Product>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _ProductPicker(),
    );
    if (product == null) return;
    final existing = _lines.where((l) => l.product.id == product.id);
    if (existing.isEmpty) {
      setState(() => _lines.add(PurchaseLine(product: product)));
    }
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _estimatedDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) setState(() => _estimatedDate = picked);
  }

  Future<void> _save() async {
    if (_provider == null) {
      _snack('Selecciona un proveedor.');
      return;
    }
    if (_lines.isEmpty) {
      _snack('Agrega al menos un producto.');
      return;
    }
    final req = PurchaseRequest(
      providerId: _provider!.id,
      providerName: _provider!.name,
      purchaseStatusId: _requestedStatusId,
      estimatedDeliveryDate: _estimatedDate,
      detail: _lines,
    );
    setState(() => _saving = true);
    try {
      await context.read<PurchaseService>().create(req);
      if (!mounted) return;
      _snack('Pedido creado.');
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _snack(String m) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(m)));
  }

  @override
  Widget build(BuildContext context) {
    // El formulario solo está operable con los catálogos ya cargados: sin
    // ellos no hay proveedor que elegir ni pedido que guardar.
    final ready = !_loading && _error == null;
    return Scaffold(
      appBar: AppBar(title: const Text('Nuevo pedido')),
      floatingActionButton: ready
          ? FloatingActionButton.extended(
              onPressed: _addProduct,
              icon: const Icon(Icons.add_shopping_cart),
              label: const Text('Producto'),
            )
          : null,
      // El total y Guardar van en la barra inferior para que el FAB quede
      // encima de ella y no tape el botón.
      bottomNavigationBar: ready ? _buildBottomBar() : null,
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(_error!),
                      TextButton(
                          onPressed: _loadCatalogs,
                          child: const Text('Reintentar')),
                    ],
                  ),
                )
              : ListView(
                  // El hueco de abajo deja que el último producto se pueda
                  // leer entero con el FAB flotando encima.
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 88),
                  children: [
                    DropdownButtonFormField<String>(
                      initialValue: _provider?.id,
                      isExpanded: true,
                      decoration: const InputDecoration(labelText: 'Proveedor'),
                      items: _providers
                          .map((p) => DropdownMenuItem(
                              value: p.id, child: Text(p.name)))
                          .toList(),
                      onChanged: (v) => setState(() =>
                          _provider = _providers.firstWhere((p) => p.id == v)),
                    ),
                    const SizedBox(height: 12),
                    // El estado no se elige: el servidor siempre crea el
                    // pedido como Solicitado y lo avanza al recibirlo.
                    if (_status != null)
                      InputDecorator(
                        decoration: const InputDecoration(
                          labelText: 'Estado',
                          enabled: false,
                        ),
                        child: Text(_status!.name),
                      ),
                    const SizedBox(height: 12),
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Icons.event),
                      title: const Text('Entrega estimada'),
                      subtitle: Text(
                          '${_estimatedDate.day.toString().padLeft(2, '0')}/${_estimatedDate.month.toString().padLeft(2, '0')}/${_estimatedDate.year}'),
                      trailing: TextButton(
                          onPressed: _pickDate, child: const Text('Cambiar')),
                    ),
                    const Divider(),
                    Text('Productos (${_lines.length})',
                        style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(height: 8),
                    if (_lines.isEmpty)
                      const Padding(
                        padding: EdgeInsets.all(16),
                        child: Center(child: Text('Aún no agregas productos.')),
                      ),
                    ..._lines.map(_buildLine),
                  ],
                ),
    );
  }

  /// Barra inferior con el total y Guardar. No usa `BottomAppBar`: ese widget
  /// impone 80px de alto y el contenido no entra, menos aún con el texto
  /// ampliado por accesibilidad. Este `Material` se mide por su contenido.
  Widget _buildBottomBar() {
    final theme = Theme.of(context);
    return Material(
      color: theme.colorScheme.surfaceContainer,
      elevation: 3,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('Total', style: TextStyle(fontSize: 18)),
                  // El monto se encoge antes que empujar a "Total" fuera de
                  // la fila cuando el total tiene muchos dígitos.
                  Flexible(
                    child: FittedBox(
                      fit: BoxFit.scaleDown,
                      alignment: Alignment.centerRight,
                      child: Text(currency(_total),
                          maxLines: 1,
                          style: const TextStyle(
                              fontSize: 22, fontWeight: FontWeight.bold)),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: _saving ? null : _save,
                icon: _saving
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.save),
                label: const Text('Guardar pedido'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLine(PurchaseLine line) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(line.product.productName,
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                ),
                IconButton(
                  icon: const Icon(Icons.delete_outline),
                  onPressed: () => setState(() => _lines.remove(line)),
                ),
              ],
            ),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    initialValue: line.orderedQuantity.toString(),
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Cantidad'),
                    onChanged: (v) => setState(
                        () => line.orderedQuantity = int.tryParse(v) ?? 1),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    initialValue: line.orderUnitPrice.toString(),
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
                    decoration:
                        const InputDecoration(labelText: 'Precio unit.'),
                    onChanged: (v) => setState(
                        () => line.orderUnitPrice = double.tryParse(v) ?? 0),
                  ),
                ),
                const SizedBox(width: 12),
                Text(currency(line.orderFinalPrice),
                    style: const TextStyle(fontWeight: FontWeight.bold)),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

/// Buscador de productos en un bottom sheet, reutilizado por el formulario.
class _ProductPicker extends StatefulWidget {
  const _ProductPicker();

  @override
  State<_ProductPicker> createState() => _ProductPickerState();
}

class _ProductPickerState extends State<_ProductPicker> {
  List<Product> _results = [];
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    _search('');
  }

  Future<void> _search(String term) async {
    setState(() => _loading = true);
    try {
      final res = await context
          .read<ProductService>()
          .getStockPaged(search: term.trim(), page: 1, pageSize: 30);
      setState(() => _results = res.items);
    } on ApiException catch (_) {
      // Silencioso en el picker.
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.7,
      maxChildSize: 0.9,
      builder: (context, scroll) => Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              autofocus: true,
              onChanged: _search,
              decoration: const InputDecoration(
                hintText: 'Buscar producto...',
                prefixIcon: Icon(Icons.search),
              ),
            ),
          ),
          if (_loading) const LinearProgressIndicator(),
          Expanded(
            child: ListView.builder(
              controller: scroll,
              itemCount: _results.length,
              itemBuilder: (context, i) {
                final p = _results[i];
                return ListTile(
                  title: Text(p.productName,
                      maxLines: 1, overflow: TextOverflow.ellipsis),
                  subtitle: Text('Stock: ${p.currentStock}'),
                  trailing: Text(currency(p.salePrice)),
                  onTap: () => Navigator.pop(context, p),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
