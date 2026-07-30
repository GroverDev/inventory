import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/theme/app_theme.dart';
import '../../models/access_menu.dart';
import '../../providers/auth_provider.dart';
import '../orders/orders_screen.dart';
import '../pos/pin_gate.dart';
import '../pos/pos_screen.dart';
import '../products/products_screen.dart';
import '../sales/sales_screen.dart';
import '../settings/settings_screen.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    // Los acentos teal del modo claro no contrastan sobre las superficies
    // oscuras, así que en dark se usan los de la paleta de la web.
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final modules = <_Module>[
      _Module(
        title: 'Productos',
        subtitle: auth.can(kProductsForm, PermAction.update)
            ? 'Buscar y modificar productos'
            : 'Consultar productos',
        icon: Icons.inventory_2_outlined,
        color: isDark ? AppDarkPalette.primary : AppPalette.color1,
        builder: (_) => const ProductsScreen(),
      ),
      _Module(
        title: 'Punto de venta',
        subtitle: 'Vender desde el móvil',
        icon: Icons.point_of_sale_outlined,
        color: isDark ? AppDarkPalette.success : AppPalette.deep,
        builder: (_) => const PinGate(child: PosScreen()),
      ),
      _Module(
        title: 'Ventas',
        subtitle: 'Ver ventas y registrar devoluciones',
        icon: Icons.receipt_outlined,
        color: isDark ? AppDarkPalette.info : AppPalette.color2,
        builder: (_) => const SalesScreen(),
      ),
      _Module(
        title: 'Pedidos',
        subtitle: 'Compras a proveedores',
        icon: Icons.receipt_long_outlined,
        color: isDark ? AppDarkPalette.warning : AppPalette.ink,
        builder: (_) => const OrdersScreen(),
      ),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Inicio'),
        actions: [
          IconButton(
            tooltip: 'Ajustes',
            icon: const Icon(Icons.settings_outlined),
            onPressed: () => Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const SettingsScreen()),
            ),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Hola, ${auth.userName.split(' ').first}',
              style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 4),
          Text('¿Qué deseas hacer?',
              style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 16),
          ...modules.map((m) => _ModuleCard(module: m)),
        ],
      ),
    );
  }
}

class _Module {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color color;
  final WidgetBuilder builder;

  _Module({
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.color,
    required this.builder,
  });
}

class _ModuleCard extends StatelessWidget {
  const _ModuleCard({required this.module});
  final _Module module;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        contentPadding: const EdgeInsets.all(12),
        leading: CircleAvatar(
          radius: 26,
          backgroundColor: module.color.withValues(alpha: 0.15),
          child: Icon(module.icon, color: module.color, size: 28),
        ),
        title: Text(module.title,
            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
        subtitle: Text(module.subtitle),
        trailing: const Icon(Icons.chevron_right),
        onTap: () => Navigator.push(
          context,
          MaterialPageRoute(builder: module.builder),
        ),
      ),
    );
  }
}
