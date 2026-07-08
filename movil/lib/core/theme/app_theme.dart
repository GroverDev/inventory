import 'package:flutter/cupertino.dart' show CupertinoPageTransitionsBuilder;
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

/// Paleta monocromática teal de la marca.
///
/// Jerarquía visual: [color1] domina acciones y componentes clave; [color2] y
/// [color3] crean capas intermedias; [color4] marca estados activos y sombras
/// suaves; [color5] es el lienzo claro de toda la app.
class AppPalette {
  AppPalette._();

  /// Dominante: acciones primarias, app bar, componentes clave.
  static const Color color1 = Color(0xFF1D9F8E);

  /// Capa secundaria: iconografía de apoyo, bordes activos.
  static const Color color2 = Color(0xFF4AB5A7);

  /// Capa terciaria: bordes en reposo, divisores, elementos pasivos.
  static const Color color3 = Color(0xFF7CC5B4);

  /// Estados activos, contenedores destacados, indicadores de selección.
  static const Color color4 = Color(0xFFA0E3D2);

  /// Capa clara para contenedores destacados (chips seleccionados, etc.).
  static const Color color5 = Color(0xFFD6F5EC);

  /// Fondo principal de las pantallas: blanco con un tinte teal casi
  /// imperceptible para que las tarjetas blancas conserven separación.
  static const Color canvas = Color(0xFFF6FCFA);

  /// Tinta de alto contraste para texto (cumple AA sobre color5 y blanco).
  static const Color ink = Color(0xFF0B3D36);

  /// Tinta atenuada para texto secundario (mantiene contraste AA).
  static const Color inkMuted = Color(0xFF3F6A61);

  /// Variante profunda de color1 para estados pressed/hover.
  static const Color deep = Color(0xFF14766A);

  /// Superficie de tarjetas y hojas sobre el lienzo teal.
  static const Color surface = Colors.white;
}

class AppTheme {
  static ThemeData light() {
    final scheme = ColorScheme.fromSeed(seedColor: AppPalette.color1).copyWith(
      primary: AppPalette.color1,
      onPrimary: Colors.white,
      primaryContainer: AppPalette.color4,
      onPrimaryContainer: AppPalette.ink,
      secondary: AppPalette.color2,
      onSecondary: Colors.white,
      secondaryContainer: AppPalette.color4,
      onSecondaryContainer: AppPalette.ink,
      tertiary: AppPalette.color3,
      onTertiary: AppPalette.ink,
      tertiaryContainer: AppPalette.color5,
      onTertiaryContainer: AppPalette.ink,
      surface: AppPalette.surface,
      onSurface: AppPalette.ink,
      onSurfaceVariant: AppPalette.inkMuted,
      surfaceContainerHighest: AppPalette.color5,
      surfaceContainerHigh: const Color(0xFFE6F9F3),
      surfaceContainer: const Color(0xFFF0FBF7),
      surfaceTint: Colors.transparent,
      outline: AppPalette.color3,
      outlineVariant: AppPalette.color4,
      inverseSurface: AppPalette.ink,
      onInverseSurface: AppPalette.color5,
      inversePrimary: AppPalette.color4,
      shadow: AppPalette.deep,
    );
    return _base(scheme, scaffold: AppPalette.canvas);
  }

  static ThemeData dark() {
    final scheme = ColorScheme.fromSeed(
      seedColor: AppPalette.color1,
      brightness: Brightness.dark,
    ).copyWith(
      primary: AppPalette.color2,
      onPrimary: const Color(0xFF06211D),
      primaryContainer: AppPalette.deep,
      onPrimaryContainer: AppPalette.color5,
      secondary: AppPalette.color3,
      onSecondary: const Color(0xFF06211D),
      secondaryContainer: const Color(0xFF14524A),
      onSecondaryContainer: AppPalette.color5,
      tertiary: AppPalette.color4,
      onTertiary: const Color(0xFF06211D),
      surface: const Color(0xFF102723),
      onSurface: const Color(0xFFDFF3EE),
      onSurfaceVariant: const Color(0xFF9CC4BB),
      surfaceContainerHighest: const Color(0xFF1B3A34),
      surfaceContainerHigh: const Color(0xFF17332E),
      surfaceContainer: const Color(0xFF132D28),
      surfaceTint: Colors.transparent,
      outline: const Color(0xFF3F6A61),
      outlineVariant: const Color(0xFF23453F),
      inversePrimary: AppPalette.color1,
    );
    return _base(scheme, scaffold: const Color(0xFF0B1F1C));
  }

  static ThemeData _base(ColorScheme scheme, {required Color scaffold}) {
    final isLight = scheme.brightness == Brightness.light;

    // Tipografía geométrica con jerarquía estricta y kerning ajustado.
    final textTheme = GoogleFonts.poppinsTextTheme().apply(
      bodyColor: scheme.onSurface,
      displayColor: scheme.onSurface,
    );
    final typography = textTheme.copyWith(
      headlineSmall: textTheme.headlineSmall?.copyWith(
        fontWeight: FontWeight.w600,
        letterSpacing: -0.4,
      ),
      titleLarge: textTheme.titleLarge?.copyWith(
        fontWeight: FontWeight.w600,
        letterSpacing: -0.3,
      ),
      titleMedium: textTheme.titleMedium?.copyWith(
        fontWeight: FontWeight.w600,
        letterSpacing: -0.1,
      ),
      titleSmall: textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
      bodyLarge: textTheme.bodyLarge?.copyWith(letterSpacing: 0),
      bodyMedium: textTheme.bodyMedium?.copyWith(letterSpacing: 0),
      bodySmall: textTheme.bodySmall?.copyWith(
        color: scheme.onSurfaceVariant,
      ),
      labelLarge: textTheme.labelLarge?.copyWith(
        fontWeight: FontWeight.w600,
        letterSpacing: 0.3,
      ),
    );

    return ThemeData(
      colorScheme: scheme,
      useMaterial3: true,
      scaffoldBackgroundColor: scaffold,
      textTheme: typography,
      splashFactory: InkSparkle.splashFactory,
      // Deslizamiento lateral estilo Cupertino en todas las plataformas:
      // la pantalla anterior queda visible debajo, sin flash de fondo.
      pageTransitionsTheme: const PageTransitionsTheme(
        builders: {
          TargetPlatform.android: CupertinoPageTransitionsBuilder(),
          TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
          TargetPlatform.windows: CupertinoPageTransitionsBuilder(),
        },
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: isLight ? AppPalette.color1 : scheme.surface,
        foregroundColor: isLight ? Colors.white : scheme.onSurface,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: typography.titleLarge?.copyWith(
          color: isLight ? Colors.white : scheme.onSurface,
          fontSize: 19,
        ),
      ),
      cardTheme: CardThemeData(
        color: scheme.surface,
        elevation: isLight ? 1.5 : 0,
        shadowColor: AppPalette.color2.withValues(alpha: isLight ? 0.35 : 0),
        surfaceTintColor: Colors.transparent,
        clipBehavior: Clip.antiAlias,
        margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
      ),
      inputDecorationTheme: InputDecorationTheme(
        isDense: true,
        filled: true,
        fillColor: scheme.surface,
        prefixIconColor: scheme.secondary,
        suffixIconColor: scheme.onSurfaceVariant,
        labelStyle: TextStyle(color: scheme.onSurfaceVariant),
        contentPadding:
            const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: scheme.outline),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: scheme.outlineVariant),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: scheme.primary, width: 1.6),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: scheme.error),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: BorderSide(color: scheme.error, width: 1.6),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: typography.labelLarge?.copyWith(fontSize: 15),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: isLight ? AppPalette.deep : scheme.primary,
          side: BorderSide(color: scheme.outline),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: typography.labelLarge,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: isLight ? AppPalette.deep : scheme.primary,
          textStyle: typography.labelLarge,
        ),
      ),
      floatingActionButtonTheme: FloatingActionButtonThemeData(
        backgroundColor: scheme.primary,
        foregroundColor: scheme.onPrimary,
        elevation: 2,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
      chipTheme: ChipThemeData(
        backgroundColor: scheme.surface,
        selectedColor: isLight ? AppPalette.color4 : scheme.primaryContainer,
        side: BorderSide(color: scheme.outlineVariant),
        labelStyle: typography.labelLarge?.copyWith(fontSize: 13),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
      listTileTheme: ListTileThemeData(
        iconColor: scheme.primary,
        titleTextStyle: typography.titleSmall?.copyWith(fontSize: 15),
        subtitleTextStyle:
            typography.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
      ),
      dividerTheme: DividerThemeData(
        color: scheme.outlineVariant,
        thickness: 1,
        space: 1,
      ),
      popupMenuTheme: PopupMenuThemeData(
        color: scheme.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 4,
        shadowColor: AppPalette.deep.withValues(alpha: 0.25),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        titleTextStyle: typography.titleLarge?.copyWith(fontSize: 18),
      ),
      bottomSheetTheme: BottomSheetThemeData(
        backgroundColor: scheme.surface,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AppPalette.ink,
        contentTextStyle:
            typography.bodyMedium?.copyWith(color: AppPalette.color5),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(
        color: scheme.primary,
      ),
      tabBarTheme: TabBarThemeData(
        labelColor: isLight ? Colors.white : scheme.primary,
        unselectedLabelColor:
            isLight ? AppPalette.color4 : scheme.onSurfaceVariant,
        indicatorColor: isLight ? AppPalette.color4 : scheme.primary,
      ),
    );
  }
}

/// Helper para formatear moneda en toda la app.
String currency(num value) => 'Bs ${value.toStringAsFixed(2)}';
