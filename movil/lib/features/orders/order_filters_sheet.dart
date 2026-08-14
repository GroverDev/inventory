import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../models/purchase.dart';

final _dayMonth = DateFormat('dd/MM');
final _dayMonthYear = DateFormat('dd/MM/yy');
final _full = DateFormat('dd/MM/yyyy');

/// Filtros con los que se consulta la lista de pedidos.
///
/// Los tres son obligatorios en `GET api/Purchases`: su SQL hace
/// `purchase_date BETWEEN` y `purchase_status_id = @PurchaseStatusId` sin
/// comodín. No existe "todos los estados" ni "sin rango" que pedir.
class PurchaseFilters {
  final int statusId;
  final DateTime from;
  final DateTime to;

  const PurchaseFilters({
    required this.statusId,
    required this.from,
    required this.to,
  });

  /// Solicitado, del primero del mes a hoy: los pedidos sobre los que hay algo
  /// que hacer. Es el mismo default con el que abre la webapp.
  factory PurchaseFilters.initial() {
    final now = DateTime.now();
    return PurchaseFilters(
      statusId: PurchaseStatusIds.requested,
      from: DateTime(now.year, now.month, 1),
      to: now,
    );
  }

  PurchaseFilters copyWith({int? statusId, DateTime? from, DateTime? to}) =>
      PurchaseFilters(
        statusId: statusId ?? this.statusId,
        from: from ?? this.from,
        to: to ?? this.to,
      );

  /// Resumen para la barra de la lista. Es lo que evita que un filtro activo
  /// se lea como si no hubiera datos.
  String get label => '${purchaseStatusLabel(statusId)} · $rangeLabel';

  String get rangeLabel {
    final format = from.year == to.year ? _dayMonth : _dayMonthYear;
    return '${format.format(from)} – ${format.format(to)}';
  }
}

/// Hoja de filtros de la lista de pedidos.
///
/// Los cinco estados no entran como chips en una fila horizontal —suman unos
/// 520dp y un teléfono normal tiene 390—, así que dos quedaban fuera de
/// pantalla sin ninguna pista. Acá se ven completos y en vertical, y el rango
/// de fechas cabe en la misma superficie.
///
/// Devuelve `null` si se descarta sin aplicar.
Future<PurchaseFilters?> showOrderFiltersSheet(
  BuildContext context,
  PurchaseFilters current,
) {
  return showModalBottomSheet<PurchaseFilters>(
    context: context,
    isScrollControlled: true,
    // Tope de altura: la hoja crece con su contenido pero deja ver algo de la
    // lista detrás, que es lo que la mantiene legible como capa temporal.
    constraints: BoxConstraints(
      maxHeight: MediaQuery.sizeOf(context).height * 0.85,
    ),
    builder: (_) => _OrderFiltersSheet(current: current),
  );
}

class _OrderFiltersSheet extends StatefulWidget {
  const _OrderFiltersSheet({required this.current});

  final PurchaseFilters current;

  @override
  State<_OrderFiltersSheet> createState() => _OrderFiltersSheetState();
}

class _OrderFiltersSheetState extends State<_OrderFiltersSheet> {
  /// Borrador: los cambios no recargan la lista hasta Aplicar, así elegir
  /// estado y fechas cuesta una sola consulta y no tres.
  late PurchaseFilters _draft = widget.current;

  Future<void> _pickFrom() async {
    final picked = await _pickDate(_draft.from);
    if (picked == null) return;
    setState(() {
      // Mover el inicio más allá del fin dejaría un rango vacío: se arrastra
      // el fin en lugar de rechazar el toque.
      _draft = _draft.copyWith(
        from: picked,
        to: picked.isAfter(_draft.to) ? picked : _draft.to,
      );
    });
  }

  Future<void> _pickTo() async {
    final picked = await _pickDate(_draft.to);
    if (picked == null) return;
    setState(() {
      _draft = _draft.copyWith(
        to: picked,
        from: picked.isBefore(_draft.from) ? picked : _draft.from,
      );
    });
  }

  Future<DateTime?> _pickDate(DateTime initial) {
    final now = DateTime.now();
    return showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(now.year - 5),
      // Un pedido no se registra con fecha futura: el calendario no la ofrece.
      lastDate: now,
    );
  }

  void _applyPreset(DateTime from) {
    setState(() => _draft = _draft.copyWith(from: from, to: DateTime.now()));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final now = DateTime.now();

    return SafeArea(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        mainAxisSize: MainAxisSize.min,
        children: [
          // Solo el contenido scrollea. Las acciones van fuera: en una pantalla
          // de 320x568 la hoja no entra entera, y "Aplicar" bajo el borde
          // obligaría a descubrirlo scrolleando.
          Flexible(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Filtros', style: theme.textTheme.titleLarge),
                  const SizedBox(height: 8),
                  _sectionTitle('Estado', theme),
                  // En vertical las etiquetas se leen enteras, que es justo lo
                  // que la fila de chips no lograba.
                  ...PurchaseStatusIds.all.map((id) {
                    final selected = _draft.statusId == id;
                    return ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(purchaseStatusLabel(id)),
                      selected: selected,
                      trailing: selected
                          ? Icon(Icons.check, color: theme.colorScheme.primary)
                          : null,
                      onTap: () =>
                          setState(() => _draft = _draft.copyWith(statusId: id)),
                    );
                  }),
                  const Divider(height: 24),
                  _sectionTitle('Periodo', theme),
                  const SizedBox(height: 8),
                  // Atajos: elegir dos fechas en el calendario del teléfono es
                  // tedioso, y estos tres cubren casi todas las consultas.
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      ActionChip(
                        label: const Text('Este mes'),
                        onPressed: () =>
                            _applyPreset(DateTime(now.year, now.month, 1)),
                      ),
                      ActionChip(
                        label: const Text('Últimos 3 meses'),
                        onPressed: () =>
                            _applyPreset(DateTime(now.year, now.month - 2, 1)),
                      ),
                      ActionChip(
                        label: const Text('Este año'),
                        onPressed: () => _applyPreset(DateTime(now.year, 1, 1)),
                      ),
                    ],
                  ),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.event),
                    title: const Text('Desde'),
                    subtitle: Text(_full.format(_draft.from)),
                    onTap: _pickFrom,
                  ),
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(Icons.event_available),
                    title: const Text('Hasta'),
                    subtitle: Text(_full.format(_draft.to)),
                    onTap: _pickTo,
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            child: Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () =>
                        setState(() => _draft = PurchaseFilters.initial()),
                    child: const Text('Restablecer'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton(
                    onPressed: () => Navigator.pop(context, _draft),
                    child: const Text('Aplicar'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _sectionTitle(String text, ThemeData theme) => Align(
        alignment: Alignment.centerLeft,
        child: Text(text,
            style: theme.textTheme.titleSmall
                ?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
      );
}
