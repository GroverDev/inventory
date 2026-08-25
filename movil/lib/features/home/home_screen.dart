import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/theme/app_theme.dart';
import '../../models/cash_session.dart';
import '../../models/sale_history.dart';
import '../../providers/auth_provider.dart';
import '../../services/sale_service.dart';
import '../pos/pin_gate.dart';
import '../pos/pos_screen.dart';

/// Pestaña Inicio: una sola acción y el estado del turno.
///
/// La lista de módulos se mudó a la barra inferior de `MainShell`, así que acá
/// queda lo que no cabe en una barra: vender, cuánto va vendido, y un aviso
/// solo cuando algo está fuera de lugar. Un aviso que aparece cuando importa se
/// lee; un número que está siempre se ignora.
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  static final _hm = DateFormat('HH:mm');
  static final _queryFmt = DateFormat('yyyy-MM-dd');

  CashSession? _session;
  SalesPage? _today;
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
    final sales = context.read<SaleService>();
    final hoy = _queryFmt.format(DateTime.now());
    try {
      // En paralelo: son independientes y así la home no tarda el doble.
      final res = await Future.wait([
        sales.activeSession(),
        sales.getSales(dateInitial: hoy, dateEnd: hoy, pageSize: 1),
      ]);
      if (!mounted) return;
      setState(() {
        _session = res[0] as CashSession?;
        _today = res[1] as SalesPage;
      });
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  /// Días completos que lleva abierta la caja, por fecha de calendario.
  int? get _diasAbierta {
    final s = _session;
    if (s == null) return null;
    final now = DateTime.now();
    final desde = DateTime(s.openedAt.year, s.openedAt.month, s.openedAt.day);
    final hoy = DateTime(now.year, now.month, now.day);
    return hoy.difference(desde).inDays;
  }

  bool get _cajaVieja => (_diasAbierta ?? 0) >= 1;

  String get _desde {
    final s = _session;
    if (s == null) return '';
    final d = _diasAbierta ?? 0;
    if (d == 0) return 'desde las ${_hm.format(s.openedAt)}';
    if (d == 1) return 'desde ayer ${_hm.format(s.openedAt)}';
    return 'hace $d días';
  }

  /// Abre el POS y, al volver, refresca: la venta o el arqueo cambiaron todo
  /// lo que muestra esta pantalla.
  Future<void> _openPos() async {
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => const PinGate(child: PosScreen())),
    );
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final aviso = _alerta();

    // El saludo es el título de la barra: el nombre de la app arriba a la
    // izquierda no le decía nada a nadie, y repetirlo en el cuerpo gastaba una
    // línea. El engranaje se fue con él: Ajustes ya vive en la pestaña "Más".
    final nombre = auth.userName.split(' ').first;

    return Scaffold(
      appBar: AppBar(
        title: Text(nombre.isEmpty ? 'Inicio' : 'Hola, $nombre'),
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
          children: [
            _sellCard(isDark),
            const SizedBox(height: 12),
            _kpis(),
            if (aviso != null) ...[
              const SizedBox(height: 12),
              aviso,
            ],
            if (_error != null) ...[
              const SizedBox(height: 12),
              _errorRow(),
            ],
          ],
        ),
      ),
    );
  }

  // ── La acción ──────────────────────────────────────────────
  Widget _sellCard(bool isDark) {
    final fondo = isDark ? AppDarkPalette.successContainer : AppPalette.deep;
    return Material(
      color: fondo,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        onTap: _openPos,
        borderRadius: BorderRadius.circular(18),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 22, 20, 18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(Icons.shopping_cart_outlined,
                      color: Colors.white, size: 30),
                  SizedBox(width: 12),
                  Text('Vender',
                      style: TextStyle(
                          color: Colors.white,
                          fontSize: 26,
                          fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 14),
              Divider(color: Colors.white.withValues(alpha: 0.22), height: 1),
              const SizedBox(height: 12),
              _sellCardStatus(),
            ],
          ),
        ),
      ),
    );
  }

  /// La línea de estado bajo "Vender". Es lo primero que hay que saber antes
  /// de tocar el botón: sin caja abierta no se puede cobrar.
  Widget _sellCardStatus() {
    const claro = TextStyle(color: Colors.white, fontSize: 12.5);
    final tenue =
        TextStyle(color: Colors.white.withValues(alpha: 0.85), fontSize: 12.5);

    if (_loading && _session == null && _error == null) {
      return Text('Consultando la caja…', style: tenue);
    }
    if (_error != null) {
      return Text('No se pudo leer el estado de la caja', style: tenue);
    }
    final s = _session;
    if (s == null) {
      return const Row(
        children: [
          Icon(Icons.lock_outline, color: Colors.white, size: 15),
          SizedBox(width: 6),
          Expanded(
            child: Text('Sin caja abierta · tocá para abrirla', style: claro),
          ),
        ],
      );
    }
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child:
              Text('Caja abierta · ${currency(s.expectedCash)}', style: claro),
        ),
        const SizedBox(width: 10),
        Text(_desde, style: tenue),
      ],
    );
  }

  // ── Las cifras ─────────────────────────────────────────────
  Widget _kpis() {
    final t = _today;
    return Row(
      children: [
        Expanded(
          child: _kpi('Vendido hoy', t == null ? '—' : currency(t.periodNet)),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _kpi('Ventas', t == null ? '—' : '${t.totalCount}'),
        ),
      ],
    );
  }

  Widget _kpi(String label, String value) => Card(
        margin: EdgeInsets.zero,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 12, 14, 13),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label.toUpperCase(),
                  style: const TextStyle(
                      fontSize: 10.5,
                      letterSpacing: 0.6,
                      fontWeight: FontWeight.bold,
                      color: Colors.grey)),
              const SizedBox(height: 2),
              Text(value,
                  style: const TextStyle(
                      fontSize: 19, fontWeight: FontWeight.bold)),
            ],
          ),
        ),
      );

  // ── El aviso ───────────────────────────────────────────────
  /// Solo cuando hay algo que hacer. Con la caja del día y todo en orden, acá
  /// no va nada.
  Widget? _alerta() {
    if (_error != null) return null;
    if (_session != null && _cajaVieja) {
      final d = _diasAbierta!;
      return _alertRow(
        icon: Icons.warning_amber_rounded,
        color: Colors.orange,
        title: d == 1
            ? 'La caja lleva 1 día abierta'
            : 'La caja lleva $d días abierta',
        subtitle: 'Conviene arquear y abrir una nueva',
        onTap: _openPos,
      );
    }
    if (!_loading && _session == null) {
      return _alertRow(
        icon: Icons.point_of_sale_outlined,
        color: Theme.of(context).colorScheme.primary,
        title: 'Abrí la caja para empezar',
        subtitle: 'Sin caja abierta no se pueden cobrar ventas',
        onTap: _openPos,
      );
    }
    return null;
  }

  Widget _alertRow({
    required IconData icon,
    required Color color,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) =>
      Card(
        margin: EdgeInsets.zero,
        child: ListTile(
          onTap: onTap,
          leading: CircleAvatar(
            backgroundColor: color.withValues(alpha: 0.14),
            child: Icon(icon, color: color),
          ),
          title:
              Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text(subtitle),
          trailing: const Icon(Icons.chevron_right),
        ),
      );

  Widget _errorRow() => Card(
        margin: EdgeInsets.zero,
        child: ListTile(
          leading: const Icon(Icons.cloud_off, color: Colors.grey),
          title: Text(_error!),
          subtitle: const Text('Las demás secciones siguen disponibles'),
          trailing: TextButton(
            onPressed: _load,
            child: const Text('Reintentar'),
          ),
        ),
      );
}
