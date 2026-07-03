import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/sale_history.dart';
import '../../services/sale_service.dart';
import 'sale_detail_screen.dart';

enum _Quick { today, week, month, custom }

class SalesScreen extends StatefulWidget {
  const SalesScreen({super.key});

  @override
  State<SalesScreen> createState() => _SalesScreenState();
}

class _SalesScreenState extends State<SalesScreen> {
  static const _pageSize = 20;
  static final _dayFmt = DateFormat('dd/MM/yyyy');
  static final _queryFmt = DateFormat('yyyy-MM-dd');

  _Quick _quick = _Quick.today;
  late DateTime _from;
  late DateTime _to;

  final List<SaleSummary> _items = [];
  int _totalCount = 0;
  double _periodTotal = 0;
  int _page = 1;

  bool _loading = false;
  bool _loadingMore = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _applyQuick(_Quick.today, reload: false);
    _load(reset: true);
  }

  // ── Rangos rápidos ─────────────────────────────────────────
  void _applyQuick(_Quick q, {bool reload = true}) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    _quick = q;
    switch (q) {
      case _Quick.today:
        _from = today;
        _to = today;
        break;
      case _Quick.week:
        // Lunes de la semana actual.
        _from = today.subtract(Duration(days: today.weekday - 1));
        _to = today;
        break;
      case _Quick.month:
        _from = DateTime(now.year, now.month, 1);
        _to = today;
        break;
      case _Quick.custom:
        break;
    }
    if (reload) _load(reset: true);
  }

  Future<void> _pickCustomRange() async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(now.year - 3),
      lastDate: DateTime(now.year, now.month, now.day),
      initialDateRange: DateTimeRange(start: _from, end: _to),
    );
    if (picked != null) {
      setState(() {
        _quick = _Quick.custom;
        _from = picked.start;
        _to = picked.end;
      });
      _load(reset: true);
    }
  }

  // ── Carga ──────────────────────────────────────────────────
  Future<void> _load({bool reset = false}) async {
    if (reset) {
      setState(() {
        _loading = true;
        _error = null;
        _page = 1;
      });
    } else {
      setState(() => _loadingMore = true);
    }

    try {
      final res = await context.read<SaleService>().getSales(
            dateInitial: _queryFmt.format(_from),
            dateEnd: _queryFmt.format(_to),
            page: _page,
            pageSize: _pageSize,
          );
      setState(() {
        if (reset) _items.clear();
        _items.addAll(res.items);
        _totalCount = res.totalCount;
        _periodTotal = res.periodTotal;
      });
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) {
        setState(() {
          _loading = false;
          _loadingMore = false;
        });
      }
    }
  }

  Future<void> _loadMore() async {
    if (_loadingMore || _items.length >= _totalCount) return;
    _page++;
    await _load();
  }

  Future<void> _openDetail(SaleSummary s) async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => SaleDetailScreen(saleId: s.id)),
    );
    // Si se registró una devolución, refrescamos la lista.
    if (changed == true) _load(reset: true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Ventas')),
      body: Column(
        children: [
          _filterBar(),
          const Divider(height: 1),
          Expanded(child: _body()),
        ],
      ),
    );
  }

  // ── UI ─────────────────────────────────────────────────────
  Widget _filterBar() {
    final rangeLabel = _from == _to
        ? _dayFmt.format(_from)
        : '${_dayFmt.format(_from)} — ${_dayFmt.format(_to)}';
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _chip('Hoy', _Quick.today),
                const SizedBox(width: 8),
                _chip('Semana', _Quick.week),
                const SizedBox(width: 8),
                _chip('Mes', _Quick.month),
                const SizedBox(width: 8),
                ActionChip(
                  avatar: const Icon(Icons.date_range, size: 18),
                  label: const Text('Rango'),
                  onPressed: _pickCustomRange,
                ),
              ],
            ),
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              const Icon(Icons.calendar_today, size: 14, color: Colors.grey),
              const SizedBox(width: 6),
              Expanded(
                child: Text(rangeLabel,
                    style: const TextStyle(fontSize: 13, color: Colors.grey)),
              ),
              if (_totalCount > 0)
                Text(
                  '$_totalCount venta(s) · ${currency(_periodTotal)}',
                  style: const TextStyle(
                      fontSize: 13, fontWeight: FontWeight.bold),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _chip(String label, _Quick q) => ChoiceChip(
        label: Text(label),
        selected: _quick == q,
        onSelected: (_) => _applyQuick(q),
      );

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
            OutlinedButton(
                onPressed: () => _load(reset: true),
                child: const Text('Reintentar')),
          ],
        ),
      );
    }
    if (_items.isEmpty) {
      return const Center(
        child: Text('No hay ventas en el período seleccionado.'),
      );
    }

    return RefreshIndicator(
      onRefresh: () => _load(reset: true),
      child: NotificationListener<ScrollNotification>(
        onNotification: (n) {
          if (n.metrics.pixels >= n.metrics.maxScrollExtent - 200) {
            _loadMore();
          }
          return false;
        },
        child: ListView.builder(
          padding: const EdgeInsets.only(bottom: 16, top: 4),
          itemCount: _items.length + 1,
          itemBuilder: (context, i) {
            if (i == _items.length) return _footer();
            return _saleCard(_items[i]);
          },
        ),
      ),
    );
  }

  Widget _footer() {
    if (_loadingMore) {
      return const Padding(
        padding: EdgeInsets.all(16),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_items.length < _totalCount) {
      return Padding(
        padding: const EdgeInsets.all(12),
        child: OutlinedButton(
            onPressed: _loadMore, child: const Text('Cargar más')),
      );
    }
    return const SizedBox(height: 8);
  }

  Widget _saleCard(SaleSummary s) {
    return Card(
      child: ListTile(
        onTap: () => _openDetail(s),
        title: Text(
          s.customerName.isEmpty ? 'Cliente' : s.customerName,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 2),
            Text(
              '${s.saleDate == null ? '—' : _dayFmt.format(s.saleDate!)}'
              '  ·  ${s.sellerName.isEmpty ? '—' : s.sellerName}',
              style: const TextStyle(fontSize: 12),
            ),
            const SizedBox(height: 4),
            _statusBadge(s.isActive),
          ],
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(currency(s.total),
                style: const TextStyle(
                    fontWeight: FontWeight.bold, fontSize: 15)),
            if (s.totalDiscounts > 0)
              Text('− ${currency(s.totalDiscounts)}',
                  style: const TextStyle(fontSize: 11, color: Colors.green)),
          ],
        ),
      ),
    );
  }

  Widget _statusBadge(bool isActive) {
    final color = isActive ? Colors.green : Colors.red;
    final label = isActive ? 'Activa' : 'Devuelta';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(label,
          style: TextStyle(
              fontSize: 11, color: color, fontWeight: FontWeight.w600)),
    );
  }
}
