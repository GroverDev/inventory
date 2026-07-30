import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/storage/pin_storage.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../providers/auth_provider.dart';
import '../../providers/theme_provider.dart';
import 'pin_change_screen.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final _pinStorage = PinStorage();
  bool _hasPin = false;

  @override
  void initState() {
    super.initState();
    _loadPinState();
  }

  Future<void> _loadPinState() async {
    final has = await _pinStorage.hasPin();
    if (mounted) setState(() => _hasPin = has);
  }

  Future<void> _openPinScreen() async {
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => PinChangeScreen(hasPin: _hasPin)),
    );
    if (!mounted) return;
    if (changed == true) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(const SnackBar(content: Text('PIN actualizado.')));
    }
    await _loadPinState();
  }

  Future<void> _confirmLogout() async {
    final ok = await confirm(
      context,
      title: 'Cerrar sesión',
      message:
          '¿Deseas salir? Se borrará el PIN del punto de venta de este dispositivo.',
      confirmLabel: 'Salir',
      destructive: true,
    );
    if (!ok || !mounted) return;
    await context.read<AuthProvider>().logout();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final theme = context.watch<ThemeProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Ajustes')),
      body: ListView(
        padding: const EdgeInsets.symmetric(vertical: 8),
        children: [
          const _SectionTitle('Cuenta'),
          Card(
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor:
                    Theme.of(context).colorScheme.primary.withValues(alpha: 0.15),
                child: Icon(Icons.person_outline,
                    color: Theme.of(context).colorScheme.primary),
              ),
              title: Text(auth.userName.isEmpty ? 'Usuario' : auth.userName),
              subtitle: auth.rolName.isEmpty ? null : Text(auth.rolName),
            ),
          ),
          const _SectionTitle('Apariencia'),
          Card(
            child: Column(
              children: [
                _ThemeOption(
                  label: 'Igual que el sistema',
                  description: 'Sigue el modo claro u oscuro del teléfono',
                  icon: Icons.brightness_auto_outlined,
                  value: ThemeMode.system,
                  selected: theme.mode,
                ),
                const Divider(),
                _ThemeOption(
                  label: 'Claro',
                  description: 'Siempre en modo claro',
                  icon: Icons.light_mode_outlined,
                  value: ThemeMode.light,
                  selected: theme.mode,
                ),
                const Divider(),
                _ThemeOption(
                  label: 'Oscuro',
                  description: 'Siempre en modo oscuro',
                  icon: Icons.dark_mode_outlined,
                  value: ThemeMode.dark,
                  selected: theme.mode,
                ),
              ],
            ),
          ),
          const _SectionTitle('Seguridad'),
          Card(
            child: ListTile(
              leading: const Icon(Icons.pin_outlined),
              title: Text(_hasPin ? 'Cambiar PIN' : 'Crear PIN'),
              subtitle: Text(_hasPin
                  ? 'PIN de acceso al punto de venta'
                  : 'Todavía no definiste un PIN para el punto de venta'),
              trailing: const Icon(Icons.chevron_right),
              onTap: _openPinScreen,
            ),
          ),
          const _SectionTitle('Sesión'),
          Card(
            child: ListTile(
              leading: Icon(Icons.logout,
                  color: Theme.of(context).colorScheme.error),
              title: Text('Cerrar sesión',
                  style:
                      TextStyle(color: Theme.of(context).colorScheme.error)),
              onTap: _confirmLogout,
            ),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(24, 20, 24, 8),
      child: Text(
        text.toUpperCase(),
        style: Theme.of(context).textTheme.labelLarge?.copyWith(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
              letterSpacing: 1,
              fontSize: 12,
            ),
      ),
    );
  }
}

class _ThemeOption extends StatelessWidget {
  const _ThemeOption({
    required this.label,
    required this.description,
    required this.icon,
    required this.value,
    required this.selected,
  });

  final String label;
  final String description;
  final IconData icon;
  final ThemeMode value;
  final ThemeMode selected;

  @override
  Widget build(BuildContext context) {
    final isSelected = value == selected;
    final scheme = Theme.of(context).colorScheme;
    return ListTile(
      leading: Icon(icon,
          color: isSelected ? scheme.primary : scheme.onSurfaceVariant),
      title: Text(label),
      subtitle: Text(description),
      trailing: isSelected ? Icon(Icons.check, color: scheme.primary) : null,
      selected: isSelected,
      onTap: () => context.read<ThemeProvider>().setMode(value),
    );
  }
}
