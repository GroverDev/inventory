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

/// Paleta del modo oscuro, tomada tal cual de la web (SmartAdmin sobre
/// Bootstrap 5.3, bloque `[data-bs-theme='dark']` de `smartapp.css`).
///
/// El comentario de cada color indica la variable CSS de origen, para poder
/// mantener ambas apps sincronizadas.
class AppDarkPalette {
  AppDarkPalette._();

  /// Lienzo detrás de los paneles: `--app-content-background`.
  static const Color canvas = Color(0xFF363C41);

  /// Paneles, tarjetas, app bar y hojas: `--app-panel-bg` / `--app-header-background`.
  static const Color surface = Color(0xFF272B30);

  /// Capa alterna, también fondo de inputs: `--bs-tertiary-bg` / `--bs-input-bg`.
  static const Color surfaceAlt = Color(0xFF2B3035);

  /// Capa destacada: `--bs-secondary-bg`.
  static const Color surfaceHigh = Color(0xFF343A40);

  /// Fondo base del documento: `--bs-body-bg`.
  static const Color body = Color(0xFF212529);

  /// Texto principal: `--bs-body-color`.
  static const Color text = Color(0xFFDEE2E6);

  /// Texto secundario: `--bs-secondary-color` (75% de opacidad, ya resuelto).
  static const Color textMuted = Color(0xFFA8ADB2);

  /// Bordes: `--bs-border-color`.
  static const Color border = Color(0xFF495057);

  /// Bordes sutiles: `--bs-border-color-translucent`.
  static const Color borderSubtle = Color(0x26FFFFFF);

  /// Acento de marca: `--bs-primary`.
  static const Color primary = Color(0xFF886AB5);

  /// Variante clara para texto y enlaces: `--bs-primary-text-emphasis` / `--bs-link-color`.
  static const Color primarySoft = Color(0xFFB8A6D3);

  /// Contenedor del acento: `--bs-primary-border-subtle`.
  static const Color primaryContainer = Color(0xFF52406D);

  /// `--bs-info`.
  static const Color info = Color(0xFF2196F3);

  /// `--bs-info-border-subtle`.
  static const Color infoContainer = Color(0xFF145A92);

  /// `--bs-success`.
  static const Color success = Color(0xFF1DC9B7);

  /// `--bs-success-border-subtle`.
  static const Color successContainer = Color(0xFF11796E);

  /// `--bs-warning`.
  static const Color warning = Color(0xFFFFC241);

  /// `--bs-danger`.
  static const Color danger = Color(0xFFFD3995);

  /// `--bs-danger-border-subtle`.
  static const Color dangerContainer = Color(0xFF982259);

  /// Borde de input con foco: `--bs-input-focus-border-color`.
  static const Color inputFocus = Color(0xFF86B7FE);
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
    return _base(
      scheme,
      scaffold: AppPalette.canvas,
      textAccent: AppPalette.deep,
    );
  }

  /// Modo oscuro con los mismos colores que la web (ver [AppDarkPalette]).
  static ThemeData dark() {
    final scheme = ColorScheme.fromSeed(
      seedColor: AppDarkPalette.primary,
      brightness: Brightness.dark,
    ).copyWith(
      primary: AppDarkPalette.primary,
      onPrimary: Colors.white,
      primaryContainer: AppDarkPalette.primaryContainer,
      onPrimaryContainer: const Color(0xFFECE5F5),
      secondary: AppDarkPalette.info,
      onSecondary: Colors.white,
      secondaryContainer: AppDarkPalette.infoContainer,
      onSecondaryContainer: const Color(0xFFDCEEFF),
      tertiary: AppDarkPalette.success,
      onTertiary: const Color(0xFF04302C),
      tertiaryContainer: AppDarkPalette.successContainer,
      onTertiaryContainer: const Color(0xFFD3F7F3),
      error: AppDarkPalette.danger,
      onError: Colors.white,
      errorContainer: AppDarkPalette.dangerContainer,
      onErrorContainer: const Color(0xFFFFE0EE),
      surface: AppDarkPalette.surface,
      onSurface: AppDarkPalette.text,
      onSurfaceVariant: AppDarkPalette.textMuted,
      surfaceContainerLowest: AppDarkPalette.body,
      surfaceContainerLow: const Color(0xFF24282C),
      surfaceContainer: AppDarkPalette.surfaceAlt,
      surfaceContainerHigh: const Color(0xFF2F353A),
      surfaceContainerHighest: AppDarkPalette.surfaceHigh,
      surfaceTint: Colors.transparent,
      outline: AppDarkPalette.border,
      outlineVariant: AppDarkPalette.borderSubtle,
      inverseSurface: AppDarkPalette.text,
      onInverseSurface: AppDarkPalette.body,
      inversePrimary: AppDarkPalette.primarySoft,
      shadow: Colors.black,
    );
    return _base(
      scheme,
      scaffold: AppDarkPalette.canvas,
      textAccent: AppDarkPalette.primarySoft,
      inputFill: AppDarkPalette.surfaceAlt,
      inputFocus: AppDarkPalette.inputFocus,
    );
  }

  static ThemeData _base(
    ColorScheme scheme, {
    required Color scaffold,

    /// Color de los elementos de acento sobre superficie (botones de texto,
    /// bordes, pestañas). En oscuro es una variante más clara del primario
    /// para no quedar por debajo del contraste AA.
    required Color textAccent,
    Color? inputFill,
    Color? inputFocus,
  }) {
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
      // Material 3 le da 80 dp de alto a la barra inferior y en un teléfono de
      // gama baja eso se come una franja notable de la pantalla. 64 dp sigue
      // siendo cómodo para el pulgar y deja las etiquetas visibles, que con
      // cuatro secciones hacen falta: los iconos solos no se adivinan.
      navigationBarTheme: NavigationBarThemeData(
        height: 64,
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        backgroundColor: isLight ? AppPalette.canvas : scheme.surface,
        indicatorColor:
            isLight ? AppPalette.color5 : scheme.primary.withValues(alpha: 0.24),
        elevation: 0,
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => typography.labelMedium?.copyWith(
            fontSize: 11.5,
            fontWeight: states.contains(WidgetState.selected)
                ? FontWeight.w700
                : FontWeight.w500,
          ),
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
        fillColor: inputFill ?? scheme.surface,
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
          borderSide:
              BorderSide(color: inputFocus ?? scheme.primary, width: 1.6),
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
          // Ancho según el contenido (Material 3). Los botones que deben
          // ocupar todo el ancho viven en columnas con
          // `CrossAxisAlignment.stretch` o dentro de un ListView, que ya les
          // dan restricciones de ancho ajustadas. 48 de alto = área táctil
          // mínima recomendada.
          minimumSize: const Size(64, 48),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: typography.labelLarge?.copyWith(fontSize: 15),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: textAccent,
          side: BorderSide(color: scheme.outline),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: typography.labelLarge,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: textAccent,
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
        shadowColor: (isLight ? AppPalette.deep : Colors.black)
            .withValues(alpha: isLight ? 0.25 : 0.45),
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
        backgroundColor:
            isLight ? AppPalette.ink : scheme.surfaceContainerHighest,
        contentTextStyle: typography.bodyMedium?.copyWith(
          color: isLight ? AppPalette.color5 : scheme.onSurface,
        ),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
      progressIndicatorTheme: ProgressIndicatorThemeData(
        color: scheme.primary,
      ),
      tabBarTheme: TabBarThemeData(
        labelColor: isLight ? Colors.white : textAccent,
        unselectedLabelColor:
            isLight ? AppPalette.color4 : scheme.onSurfaceVariant,
        indicatorColor: isLight ? AppPalette.color4 : textAccent,
      ),
    );
  }
}

/// Helper para formatear moneda en toda la app.
String currency(num value) => 'Bs ${value.toStringAsFixed(2)}';
