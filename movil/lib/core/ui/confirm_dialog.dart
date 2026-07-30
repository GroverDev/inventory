import 'package:flutter/material.dart';

/// Diálogo de confirmación estándar de la app.
///
/// Sigue la convención de Material 3: las acciones van alineadas a la derecha
/// y cada una mide lo que mide su texto — la jerarquía la dan la posición y el
/// color, no el tamaño. La acción que descarta va primero (izquierda) y la que
/// confirma después (derecha).
///
/// Con [destructive] la confirmación se pinta con el color de error, para que
/// se lea como irreversible sin quedar más fácil de acertar que Cancelar.
///
/// Devuelve `false` si el usuario cancela o descarta el diálogo tocando fuera.
Future<bool> confirm(
  BuildContext context, {
  required String title,
  required String message,
  String confirmLabel = 'Aceptar',
  String cancelLabel = 'Cancelar',
  bool destructive = false,
}) async {
  final scheme = Theme.of(context).colorScheme;
  final result = await showDialog<bool>(
    context: context,
    builder: (dialogContext) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(dialogContext, false),
          child: Text(cancelLabel),
        ),
        FilledButton(
          style: destructive
              ? FilledButton.styleFrom(
                  backgroundColor: scheme.error,
                  foregroundColor: scheme.onError,
                )
              : null,
          onPressed: () => Navigator.pop(dialogContext, true),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return result ?? false;
}
