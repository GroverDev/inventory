import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';
import 'package:inventory_movil/core/ui/confirm_dialog.dart';

Widget _host({required ThemeData theme}) => MaterialApp(
      theme: theme,
      home: Scaffold(
        body: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => confirm(
              context,
              title: 'Cerrar sesión',
              message: '¿Deseas salir?',
              confirmLabel: 'Salir',
              destructive: true,
            ),
            child: const Text('open'),
          ),
        ),
      ),
    );

void main() {
  testWidgets('las acciones del diálogo miden lo que su texto', (tester) async {
    await tester.pumpWidget(_host(theme: AppTheme.light()));
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    final dialog = tester.getSize(find.byType(AlertDialog));
    final cancel = tester.getSize(find.widgetWithText(TextButton, 'Cancelar'));
    final accept = tester.getSize(find.widgetWithText(FilledButton, 'Salir'));

    // El botón de confirmar no puede acaparar el ancho del diálogo...
    expect(accept.width, lessThan(dialog.width / 2));
    // ...ni ser desproporcionado frente al de cancelar: la diferencia debe
    // venir del largo de la etiqueta, no de un minimumSize infinito.
    expect(accept.width, lessThan(cancel.width * 2));
    // Altura acotada al área táctil mínima del tema.
    expect(accept.height, lessThanOrEqualTo(48));
  });

  testWidgets('la confirmación destructiva usa el color de error',
      (tester) async {
    final theme = AppTheme.dark();
    await tester.pumpWidget(_host(theme: theme));
    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    final button = tester.widget<FilledButton>(
        find.widgetWithText(FilledButton, 'Salir'));
    final background = button.style?.backgroundColor
        ?.resolve(<WidgetState>{});
    expect(background, theme.colorScheme.error);
  });
}
