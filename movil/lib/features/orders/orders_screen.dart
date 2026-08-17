import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/purchase.dart';
import '../../services/purchase_service.dart';
import 'order_filters_sheet.dart';
import 'order_form_screen.dart';
import 'order_receive_screen.dart';

class OrdersScreen extends StatefulWidget {
  const OrdersScreen({super.key});

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> {
  List<PurchaseSummary> _items = [];
  bool _loading = true;
  String? _error;

  PurchaseFilters _filters = PurchaseFilters.initial();

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
      _items = await context.read<PurchaseService>().list(
            from: _filters.from,
            to: _filters.to,
            statusId: _filters.statusId,
          );
    } on ApiException catch (e) {
      _error = e.message;
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _openFilters() async {
    final applied = await showOrderFiltersSheet(context, _filters);
    if (applied == null) return;
    setState(() => _filters = applied);
    _load();
  }

  Future<void> _create() async {
    final created = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => const OrderFormScreen()),
    );
    if (created == true) _load();
  }

  /// Abre la recepción. También para un pedido que ya no admite recibir: ahí la
  /// pantalla sirve para consultar qué llegó, y lo explica en lugar de dejar la
  /// fila muerta al tacto.
  Future<void> _open(PurchaseSummary purchase) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => OrderReceiveScreen(purchaseId: purchase.id),
      ),
    );
    // Tras recibir, el pedido cambia de estado y probablemente ya no pertenece
    // al filtro actual: recargar es lo que lo saca (o lo deja) donde toca.
    if (changed == true) _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Pedidos')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _create,
        icon: const Icon(Icons.add),
        label: const Text('Nuevo pedido'),
      ),
      body: Column(
        children: [
          _buildFilterBar(),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  /// Barra que muestra el filtro vigente y abre la hoja al tocarla.
  ///
  /// Deja el filtro a la vista en todo momento: la lista nunca son "todos los
  /// pedidos", y confundir un filtro activo con ausencia de datos ya pasó una
  /// vez. No lleva alto fijo para que crezca con el texto ampliado.
  Widget _buildFilterBar() {
    final theme = Theme.of(context);
    return Material(
      color: theme.colorScheme.surfaceContainer,
      child: InkWell(
        onTap: _openFilters,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            children: [
              Icon(Icons.tune, size: 20, color: theme.colorScheme.primary),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  _filters.label,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.titleSmall,
                ),
              ),
              const SizedBox(width: 8),
              Icon(Icons.expand_more,
                  size: 20, color: theme.colorScheme.onSurfaceVariant),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBody() {
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
      // El texto nombra el filtro completo, estado y periodo: así el vacío se
      // lee como "acá no hay", no como "no hay pedidos".
      return Center(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                // Con "Todos" nombrar el estado sobra y suena a trabalenguas:
                // ahí lo único que acota la búsqueda es el periodo.
                _filters.statusId == PurchaseStatusIds.todos
                    ? 'Sin pedidos entre ${_filters.rangeLabel}.'
                    : 'Sin pedidos en "${purchaseStatusLabel(_filters.statusId)}" '
                        'entre ${_filters.rangeLabel}.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: _openFilters,
                icon: const Icon(Icons.tune),
                label: const Text('Cambiar filtros'),
              ),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _load,
      child: ListView.builder(
        itemCount: _items.length,
        itemBuilder: (context, i) {
          final p = _items[i];
          return Card(
            child: ListTile(
              leading: const CircleAvatar(child: Icon(Icons.receipt_long)),
              title: Text(
                  p.providerName.isEmpty ? 'Proveedor' : p.providerName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis),
              subtitle: Text('${p.purchaseDate}  ·  ${p.statusName}'),
              trailing: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(currency(p.total),
                      style: const TextStyle(fontWeight: FontWeight.bold)),
                  if (canReceivePurchase(p.statusId))
                    Text('Recibir',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: Theme.of(context).colorScheme.primary,
                              fontWeight: FontWeight.w600,
                            )),
                ],
              ),
              onTap: () => _open(p),
            ),
          );
        },
      ),
    );
  }
}
