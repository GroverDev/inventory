import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../core/utils/uid.dart';
import '../../models/purchase.dart';
import '../../services/purchase_service.dart';

/// Recepción de mercadería de un pedido.
///
/// Equivale a `PurchaseReceiveView.vue` de la webapp, pero la tabla de siete
/// columnas no cabe acá: cada producto es una tarjeta con sus saldos y los dos
/// campos editables (cantidad recibida y precio facturado).
///
/// Ninguna regla de negocio vive en esta pantalla. El servidor revalida todo
/// dentro de la transacción que escribe (`PurchaseReceiptPolicy`); lo de acá
/// solo evita el viaje y le explica al usuario qué no cuadra.
class OrderReceiveScreen extends StatefulWidget {
  const OrderReceiveScreen({super.key, required this.purchaseId});

  final String purchaseId;

  @override
  State<OrderReceiveScreen> createState() => _OrderReceiveScreenState();
}

class _OrderReceiveScreenState extends State<OrderReceiveScreen> {
  static final _display = DateFormat('dd/MM/yyyy');

  PurchaseOrder? _order;
  List<PurchaseDeliveryLine> _lines = [];
  final Map<String, _LineEditors> _editors = {};

  DateTime _deliveryDate = DateTime.now();

  /// Se genera una sola vez por pantalla y sobrevive a los reintentos: es lo
  /// que hace idempotente el envío. Un rechazo no lo consume — la transacción
  /// se revierte entera —, así que corregir cantidades y reenviar es seguro.
  final String _operationUid = newUid();

  bool _loading = true;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final e in _editors.values) {
      e.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final order = await context.read<PurchaseService>().getById(
            widget.purchaseId,
          );
      final lines = order.detail.map(PurchaseDeliveryLine.new).toList();

      for (final e in _editors.values) {
        e.dispose();
      }
      _editors
        ..clear()
        ..addEntries(lines.map(
          (l) => MapEntry(l.source.productId, _LineEditors(l)),
        ));

      setState(() {
        _order = order;
        _lines = lines;
      });
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  double get _total => _lines.fold(0, (s, l) => s + l.subtotal);

  bool get _canSubmit =>
      (_order?.hasPending ?? false) && _lines.any((l) => l.deliveryQuantity > 0);

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _deliveryDate,
      firstDate: now.subtract(const Duration(days: 365)),
      // El servidor rechaza una fecha futura; el calendario ni la ofrece.
      lastDate: now,
    );
    if (picked != null) setState(() => _deliveryDate = picked);
  }

  Future<void> _submit() async {
    final delivery = PurchaseDelivery(
      purchaseId: widget.purchaseId,
      deliveryDate: _deliveryDate,
      detail: _lines,
      operationUid: _operationUid,
    );

    if (delivery.receivedLines.isEmpty) {
      _snack('Indica la cantidad recibida de al menos un producto.');
      return;
    }

    // Sin lote el servidor rechaza la entrega COMPLETA, no solo esa línea: se
    // corta acá para que no haya que volver a cargar todo lo demás.
    final sinLote = delivery.linesMissingLot;
    if (sinLote.isNotEmpty) {
      _snack('"${sinLote.first.source.productName}" se maneja por lotes: '
          'indica el lote recibido.');
      return;
    }

    // Una unidad, un número: el servidor rechaza la entrega completa si no
    // coinciden, así que se corta acá.
    final seriesMal = delivery.linesWithSerialMismatch;
    if (seriesMal.isNotEmpty) {
      final l = seriesMal.first;
      _snack('"${l.source.productName}" se identifica por número de serie: '
          'indica ${l.deliveryQuantity} número(s), hay ${l.serialNumbers.length}.');
      return;
    }

    // Recibir mercadería vencida casi siempre es un error de tipeo, pero a
    // veces es real. Se advierte, no se prohíbe.
    final vencidas = delivery.receivedLines
        .where((l) =>
            l.expiryDate != null && l.expiryDate!.isBefore(DateTime.now()))
        .toList();
    if (vencidas.isNotEmpty) {
      final seguir = await confirm(
        context,
        title: 'Vencimiento cumplido',
        message: 'El vencimiento indicado en '
            '"${vencidas.first.source.productName}" ya pasó. '
            '¿Registrar la recepción igual?',
        confirmLabel: 'Registrar',
      );
      if (!seguir || !mounted) return;
    }

    final ok = await confirm(
      context,
      title: 'Confirmar recepción',
      message: delivery.isPartial
          ? 'Esta recepción es parcial: el pedido quedará con saldo pendiente. '
              '¿Continuar?'
          : '¿Confirmas la recepción de este pedido?',
      confirmLabel: 'Confirmar',
    );
    if (!ok || !mounted) return;

    setState(() => _saving = true);
    try {
      final result = await context.read<PurchaseService>().receive(delivery);
      if (!mounted) return;
      _snack(switch (result.outcome) {
        PurchaseReceiptOutcome.applied => 'Recepción registrada.',
        // El envío anterior sí había entrado: el stock ya está, no se repite.
        PurchaseReceiptOutcome.alreadyRegistered => result.message,
      });
      Navigator.pop(context, true);
    } on ApiException catch (e) {
      _snack(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _close() async {
    final ok = await confirm(
      context,
      title: 'Cerrar con faltante',
      message: 'El saldo pendiente ya no se podrá recibir. ¿Cerrar el pedido?',
      confirmLabel: 'Cerrar',
      destructive: true,
    );
    if (!ok || !mounted) return;

    setState(() => _saving = true);
    try {
      await context.read<PurchaseService>().close(widget.purchaseId);
      if (!mounted) return;
      _snack('Pedido cerrado con faltante.');
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
    final order = _order;
    final ready = !_loading && _error == null && order != null;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Recepción'),
        actions: [
          if (ready && canClosePurchase(order.statusId))
            PopupMenuButton<String>(
              onSelected: (_) => _close(),
              itemBuilder: (_) => const [
                PopupMenuItem(
                  value: 'close',
                  child: ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(Icons.lock_outline),
                    title: Text('Cerrar con faltante'),
                  ),
                ),
              ],
            ),
        ],
      ),
      bottomNavigationBar: ready && order.hasPending ? _buildBottomBar() : null,
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildError(_error!)
              : order == null
                  ? const SizedBox.shrink()
                  : ListView(
                      // Hueco inferior: la última tarjeta se lee entera sobre
                      // la barra de confirmación.
                      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
                      children: [
                        _buildHeader(order),
                        const SizedBox(height: 8),
                        if (!order.hasPending) _buildNothingPending(),
                        if (!canReceivePurchase(order.statusId))
                          _buildNotReceivable(order),
                        const Divider(height: 24),
                        Text('Productos (${_lines.length})',
                            style: Theme.of(context).textTheme.titleMedium),
                        const SizedBox(height: 8),
                        ..._lines.map(_buildLine),
                      ],
                    ),
    );
  }

  Widget _buildError(String message) => Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 48, color: Colors.grey),
            const SizedBox(height: 12),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: Text(message, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Reintentar')),
          ],
        ),
      );

  Widget _buildHeader(PurchaseOrder order) {
    final theme = Theme.of(context);
    final purchaseDate = DateTime.tryParse(order.purchaseDate);

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              order.providerName.isEmpty ? 'Proveedor' : order.providerName,
              style: theme.textTheme.titleMedium,
            ),
            const SizedBox(height: 4),
            Text(
              'Pedido del '
              '${purchaseDate != null ? _display.format(purchaseDate) : order.purchaseDate}',
              style: theme.textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                Chip(
                  label: Text(order.statusName.isEmpty
                      ? purchaseStatusLabel(order.statusId)
                      : order.statusName),
                  visualDensity: VisualDensity.compact,
                ),
                Text('Total del pedido: ${currency(order.total)}',
                    style: theme.textTheme.bodySmall),
              ],
            ),
            const Divider(height: 24),
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.event_available),
              title: const Text('Fecha de recepción'),
              subtitle: Text(_display.format(_deliveryDate)),
              trailing: TextButton(
                onPressed: _saving ? null : _pickDate,
                child: const Text('Cambiar'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildNothingPending() => const Card(
        margin: EdgeInsets.only(top: 8),
        child: ListTile(
          leading: Icon(Icons.info_outline),
          title: Text('Este pedido no tiene saldo pendiente de recepción.'),
        ),
      );

  Widget _buildNotReceivable(PurchaseOrder order) => Card(
        margin: const EdgeInsets.only(top: 8),
        child: ListTile(
          leading: const Icon(Icons.block),
          title: Text(
            'Un pedido en estado '
            '"${purchaseStatusLabel(order.statusId)}" ya no admite recepciones.',
          ),
        ),
      );

  /// Barra inferior con el total y Confirmar, mismo patrón que el formulario de
  /// pedido: el contenido la mide, así no se corta con el texto ampliado.
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
                  const Text('Total recibido', style: TextStyle(fontSize: 18)),
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
                onPressed: _saving || !_canSubmit ? null : _submit,
                icon: _saving
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.inventory_2_outlined),
                label: const Text('Confirmar recepción'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLine(PurchaseDeliveryLine line) {
    final theme = Theme.of(context);
    final editors = _editors[line.source.productId]!;
    final complete = line.source.pendingQuantity == 0;

    return Opacity(
      // La línea sin saldo se muestra igual —el usuario necesita ver qué ya
      // llegó— pero atenuada y sin campos editables.
      opacity: complete ? 0.6 : 1,
      child: Card(
        margin: const EdgeInsets.only(bottom: 8),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(line.source.productName,
                        style: const TextStyle(fontWeight: FontWeight.w600)),
                  ),
                  if (line.usesLot || line.usesSerial) ...[
                    const SizedBox(width: 8),
                    _trackingChip(theme, line.usesLot ? 'Lote' : 'Series'),
                  ],
                ],
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 4,
                children: [
                  _saldo('Ordenado', line.source.orderedQuantity, theme),
                  _saldo('Recibido', line.source.receivedQuantity, theme),
                  _saldo('Pendiente', line.source.pendingQuantity, theme,
                      highlight: !complete),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: TextField(
                      controller: editors.quantity,
                      enabled: !complete && !_saving,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(labelText: 'A recibir'),
                      onChanged: (v) => _onQuantityChanged(line, editors, v),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextField(
                      controller: editors.price,
                      enabled: !complete && !_saving,
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      decoration:
                          const InputDecoration(labelText: 'Precio unit.'),
                      onChanged: (v) => setState(
                          () => line.unitPrice = double.tryParse(v) ?? 0),
                    ),
                  ),
                ],
              ),
              // Los campos del lote solo aparecen donde hacen falta: en una
              // tarjeta ya cargada, sumárselos a todos los productos es ruido.
              if (line.usesLot && !complete) ...[
                const SizedBox(height: 12),
                _buildLotFields(line, editors, theme),
              ],
              if (line.usesSerial && !complete) ...[
                const SizedBox(height: 12),
                _buildSerialFields(line, editors, theme),
              ],
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerRight,
                child: Text(currency(line.subtotal),
                    style: const TextStyle(fontWeight: FontWeight.bold)),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _trackingChip(ThemeData theme, String texto) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(
          color: theme.colorScheme.tertiaryContainer,
          borderRadius: BorderRadius.circular(6),
        ),
        child: Text(
          texto,
          style: theme.textTheme.labelSmall?.copyWith(
            color: theme.colorScheme.onTertiaryContainer,
            fontWeight: FontWeight.w600,
          ),
        ),
      );

  /// Lote y vencimiento de una línea. Van uno debajo del otro y no en fila:
  /// el código de lote es largo y en un teléfono angosto quedaría ilegible
  /// compartiendo el ancho con una fecha.
  Widget _buildLotFields(
    PurchaseDeliveryLine line,
    _LineEditors editors,
    ThemeData theme,
  ) {
    final expiry = line.expiryDate;
    final vencido = expiry != null && expiry.isBefore(DateTime.now());

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextField(
          controller: editors.lot,
          enabled: !_saving,
          textCapitalization: TextCapitalization.characters,
          maxLength: 50,
          decoration: const InputDecoration(
            labelText: 'Lote recibido *',
            hintText: 'Código impreso en la caja',
            counterText: '',
          ),
          onChanged: (v) => setState(() => line.lotCode = v),
        ),
        const SizedBox(height: 4),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: _saving ? null : () => _pickExpiry(line),
                icon: const Icon(Icons.event_outlined, size: 18),
                label: Text(
                  expiry == null
                      ? 'Vencimiento (opcional)'
                      : 'Vence: ${_display.format(expiry)}',
                ),
              ),
            ),
            if (expiry != null)
              IconButton(
                tooltip: 'Quitar vencimiento',
                onPressed:
                    _saving ? null : () => setState(() => line.expiryDate = null),
                icon: const Icon(Icons.close),
              ),
          ],
        ),
        if (vencido)
          Text(
            'Ese vencimiento ya pasó.',
            style: theme.textTheme.bodySmall
                ?.copyWith(color: theme.colorScheme.error),
          ),
        Text(
          'Un lote por recepción. Si llegaron varios, registra este y repite '
          'la recepción con el saldo.',
          style: theme.textTheme.bodySmall
              ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
      ],
    );
  }

  /// Números de serie: uno por línea, en un campo multilínea.
  ///
  /// Un lector de códigos emite Enter al final de cada lectura, así que este
  /// campo se llena de corrido sin tocar el teclado. Por eso multilínea y no N
  /// campos separados: con N campos habría que ir dando foco a cada uno.
  Widget _buildSerialFields(
    PurchaseDeliveryLine line,
    _LineEditors editors,
    ThemeData theme,
  ) {
    final faltan = line.deliveryQuantity - line.serialNumbers.length;
    final expiry = line.expiryDate;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextField(
          controller: editors.serials,
          enabled: !_saving,
          maxLines: 4,
          minLines: 2,
          textCapitalization: TextCapitalization.characters,
          decoration: InputDecoration(
            labelText: 'Números de serie *',
            helperText: 'Uno por línea, o léelos con el lector',
            counterText:
                '${line.serialNumbers.length} de ${line.deliveryQuantity}',
            errorText: faltan == 0
                ? null
                : (faltan > 0 ? 'Faltan $faltan' : 'Sobran ${-faltan}'),
          ),
          onChanged: (v) => setState(() {
            line.serialNumbers = v
                .split('\n')
                .map((x) => x.trim())
                .where((x) => x.isNotEmpty)
                .toList();
          }),
        ),
        const SizedBox(height: 4),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: _saving ? null : () => _pickExpiry(line),
                icon: const Icon(Icons.event_outlined, size: 18),
                label: Text(
                  expiry == null
                      ? 'Vencimiento (opcional)'
                      : 'Vence: ${_display.format(expiry)}',
                ),
              ),
            ),
            if (expiry != null)
              IconButton(
                tooltip: 'Quitar vencimiento',
                onPressed:
                    _saving ? null : () => setState(() => line.expiryDate = null),
                icon: const Icon(Icons.close),
              ),
          ],
        ),
        Text(
          'Un número por unidad: identifica cuál se entregó ante una garantía.',
          style: theme.textTheme.bodySmall
              ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
      ],
    );
  }

  Future<void> _pickExpiry(PurchaseDeliveryLine line) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: line.expiryDate ?? now.add(const Duration(days: 365)),
      // Se permite el pasado: a veces llega mercadería ya vencida y el sistema
      // tiene que poder registrarlo para que alguien lo vea y lo devuelva.
      firstDate: DateTime(now.year - 5),
      lastDate: DateTime(now.year + 15),
    );
    if (picked != null) setState(() => line.expiryDate = picked);
  }

  /// Recorta la cantidad al pendiente mientras el usuario tipea. Solo reescribe
  /// el campo cuando se pasa: corregirlo en cada tecla le arruinaría el número
  /// a medio escribir.
  void _onQuantityChanged(
    PurchaseDeliveryLine line,
    _LineEditors editors,
    String value,
  ) {
    final parsed = int.tryParse(value) ?? 0;
    final clamped = parsed.clamp(0, line.source.pendingQuantity);

    if (clamped != parsed) {
      final text = clamped.toString();
      editors.quantity.value = TextEditingValue(
        text: text,
        selection: TextSelection.collapsed(offset: text.length),
      );
      _snack(
        'De "${line.source.productName}" solo quedan '
        '${line.source.pendingQuantity} por recibir.',
      );
    }
    setState(() => line.deliveryQuantity = clamped);
  }

  Widget _saldo(String label, int value, ThemeData theme,
      {bool highlight = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: highlight
            ? theme.colorScheme.primaryContainer
            : theme.colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        '$label: $value',
        style: theme.textTheme.bodySmall?.copyWith(
          fontWeight: highlight ? FontWeight.w600 : FontWeight.w400,
          color: highlight
              ? theme.colorScheme.onPrimaryContainer
              : theme.colorScheme.onSurfaceVariant,
        ),
      ),
    );
  }
}

/// Campos de texto de una línea. Van en controladores —y no en `initialValue`—
/// porque la cantidad se reescribe sola al recortarla contra el pendiente.
class _LineEditors {
  _LineEditors(PurchaseDeliveryLine line)
      : quantity = TextEditingController(text: line.deliveryQuantity.toString()),
        price = TextEditingController(
            text: line.unitPrice.toStringAsFixed(2)),
        lot = TextEditingController(text: line.lotCode),
        serials = TextEditingController(text: line.serialNumbers.join('\n'));

  final TextEditingController quantity;
  final TextEditingController price;
  final TextEditingController lot;
  final TextEditingController serials;

  void dispose() {
    quantity.dispose();
    price.dispose();
    lot.dispose();
    serials.dispose();
  }
}
