import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';

Widget _sheetBody() => Padding(
      padding: const EdgeInsets.only(bottom: 0),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text('Cobrar venta'),
            Row(
              children: [
                Expanded(child: TextField(controller: TextEditingController())),
                const SizedBox(width: 8),
                FilledButton.tonal(
                    onPressed: () {}, child: const Text('Agregar')),
              ],
            ),
          ],
        ),
      ),
    );

void main() {
  testWidgets('payment sheet layout does not overflow width', (tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light(),
      home: Scaffold(
        body: Builder(
          builder: (context) => ElevatedButton(
            onPressed: () => showModalBottomSheet(
              context: context,
              isScrollControlled: true,
              showDragHandle: true,
              builder: (_) => _sheetBody(),
            ),
            child: const Text('open'),
          ),
        ),
      ),
    ));

    await tester.tap(find.text('open'));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Agregar'), findsOneWidget);
  });
}
