import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/ui/confirm_dialog.dart';
import '../../providers/auth_provider.dart';
import '../../providers/cart_provider.dart';

/// Lista las sucursales habilitadas y pasa la sesión a la elegida.
///
/// Cambiar vacía el carrito (sus precios y stock son de la sucursal anterior)
/// y reconstruye las pestañas. La caja abierta no se toca: es de su sucursal y
/// sigue abierta ahí hasta que se vuelva a cerrarla.
Future<void> showBranchPicker(BuildContext context) async {
  final auth = context.read<AuthProvider>();
  final elegida = await showModalBottomSheet<String>(
    context: context,
    showDragHandle: true,
    builder: (ctx) => SafeArea(
      child: ListView(
        shrinkWrap: true,
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: Text('Sucursal',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
          ),
          for (final b in auth.branches)
            ListTile(
              leading: Icon(b.branchId == auth.branchId
                  ? Icons.radio_button_checked
                  : Icons.radio_button_unchecked),
              title: Text(b.name),
              subtitle: b.isDefault ? const Text('Por defecto') : null,
              onTap: () => Navigator.pop(ctx, b.branchId),
            ),
        ],
      ),
    ),
  );
  if (elegida == null || elegida == auth.branchId || !context.mounted) return;

  if (context.read<CartProvider>().lines.isNotEmpty) {
    final ok = await confirm(
      context,
      title: 'Cambiar de sucursal',
      message: 'El carrito tiene productos y se va a vaciar: los precios y el '
          'stock son de cada sucursal.',
      confirmLabel: 'Cambiar',
    );
    if (!ok || !context.mounted) return;
  }

  final error = await auth.switchBranch(elegida);
  if (error != null && context.mounted) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(error)));
  }
}
