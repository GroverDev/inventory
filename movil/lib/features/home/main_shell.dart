import 'package:flutter/material.dart';

import '../cash/cash_sessions_screen.dart';
import '../orders/orders_screen.dart';
import '../products/products_screen.dart';
import '../sales/sales_screen.dart';
import '../settings/settings_screen.dart';
import 'home_screen.dart';

/// Cáscara de la app: barra inferior fija + las pestañas raíz.
///
/// Hay un solo `Navigator`, el de `MaterialApp`. Las pantallas de detalle
/// (ficha de producto, detalle de venta, cobro) se siguen abriendo con
/// `Navigator.push` y tapan la barra, que es el comportamiento estándar de
/// Material: la barra es para moverse entre secciones, no para escaparse de
/// una tarea a medio hacer. Un `Navigator` por pestaña haría que la barra
/// nunca se tape, pero obliga a que cada `push` apunte al navigator correcto
/// y no vale la complejidad para cuatro secciones.
class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _index = 0;

  /// Pestañas ya visitadas. `IndexedStack` construye todos sus hijos de una,
  /// y Productos y Ventas cargan datos en `initState`: sin esto, abrir la app
  /// dispararía dos llamadas que quizá nadie vaya a mirar. Una vez visitada,
  /// la pestaña queda viva y conserva su scroll y sus filtros.
  final Set<int> _visited = {0};

  void _go(int i) {
    setState(() {
      _index = i;
      _visited.add(i);
    });
  }

  Widget _tab(int i) => switch (i) {
        0 => const HomeScreen(),
        1 => const ProductsScreen(),
        2 => const SalesScreen(),
        _ => const _MoreTab(),
      };

  @override
  Widget build(BuildContext context) {
    return PopScope(
      // Estando en otra pestaña, el botón atrás de Android vuelve a Inicio en
      // vez de cerrar la app.
      canPop: _index == 0,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) _go(0);
      },
      child: Scaffold(
        // La barra inferior ya consume el inset de abajo; sin esto los
        // Scaffold de cada pestaña vuelven a aplicarlo y dejan un hueco.
        body: MediaQuery.removePadding(
          context: context,
          removeBottom: true,
          child: IndexedStack(
            index: _index,
            children: [
              for (var i = 0; i < 4; i++)
                _visited.contains(i) ? _tab(i) : const SizedBox.shrink(),
            ],
          ),
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: _index,
          onDestinationSelected: _go,
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.home_outlined),
              selectedIcon: Icon(Icons.home),
              label: 'Inicio',
            ),
            NavigationDestination(
              icon: Icon(Icons.inventory_2_outlined),
              selectedIcon: Icon(Icons.inventory_2),
              label: 'Productos',
            ),
            NavigationDestination(
              icon: Icon(Icons.receipt_outlined),
              selectedIcon: Icon(Icons.receipt),
              label: 'Ventas',
            ),
            NavigationDestination(
              icon: Icon(Icons.more_horiz),
              label: 'Más',
            ),
          ],
        ),
      ),
    );
  }
}

/// Lo que no entra en la barra: se usa una o dos veces por jornada.
class _MoreTab extends StatelessWidget {
  const _MoreTab();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Más')),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          _tile(
            context,
            icon: Icons.account_balance_wallet_outlined,
            title: 'Sesiones de caja',
            subtitle: 'Aperturas, cierres y arqueo',
            builder: (_) => const CashSessionsScreen(),
          ),
          _tile(
            context,
            icon: Icons.receipt_long_outlined,
            title: 'Pedidos',
            subtitle: 'Compras a proveedores',
            builder: (_) => const OrdersScreen(),
          ),
          const Divider(height: 24),
          _tile(
            context,
            icon: Icons.settings_outlined,
            title: 'Ajustes',
            subtitle: 'Tema, PIN y sesión',
            builder: (_) => const SettingsScreen(),
          ),
        ],
      ),
    );
  }

  Widget _tile(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String subtitle,
    required WidgetBuilder builder,
  }) =>
      ListTile(
        leading: Icon(icon),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(subtitle),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => Navigator.push(
          context,
          MaterialPageRoute(builder: builder),
        ),
      );
}
