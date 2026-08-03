import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/theme/app_theme.dart';

/// La tipografía viaja dentro de la app: `google_fonts/Poppins-Regular.ttf`
/// declarado como asset, y `allowRuntimeFetching = false` en `main()`.
///
/// Si alguien mueve o renombra el archivo, la app no falla de forma visible:
/// se dibuja con la fuente por defecto del sistema. Estas pruebas convierten
/// ese silencio en un test rojo.
void main() {
  // `rootBundle` necesita el binding; sin esto falla antes de leer el asset.
  TestWidgetsFlutterBinding.ensureInitialized();

  test('Poppins regular viaja empaquetada, con el nombre que espera '
      'google_fonts', () async {
    // El nombre importa: el paquete resuelve el asset buscando uno que termine
    // en `Poppins-Regular.ttf`. Renombrarlo lo manda a la red.
    final bytes = await rootBundle.load('google_fonts/Poppins-Regular.ttf');
    expect(bytes.lengthInBytes, greaterThan(100000));
  });

  test('el tema solo pide la variante regular', () {
    // Las negritas de la app (`FontWeight.bold`, `w600`) se dibujan
    // engrosando la regular, no con archivos Medium/SemiBold/Bold. Si esto
    // cambia, hay que empaquetar también esas variantes o dejarán de verse.
    final familias = <String>{};
    for (final tema in [AppTheme.light(), AppTheme.dark()]) {
      final t = tema.textTheme;
      for (final estilo in <TextStyle?>[
        t.displayLarge, t.displayMedium, t.displaySmall,
        t.headlineLarge, t.headlineMedium, t.headlineSmall,
        t.titleLarge, t.titleMedium, t.titleSmall,
        t.bodyLarge, t.bodyMedium, t.bodySmall,
        t.labelLarge, t.labelMedium, t.labelSmall,
      ]) {
        if (estilo?.fontFamily != null) familias.add(estilo!.fontFamily!);
      }
    }
    expect(familias, {'Poppins_regular'});
  });
}
