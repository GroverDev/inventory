import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

import 'core/config/app_config.dart';
import 'core/navigation/navigator_key.dart';
import 'core/network/api_client.dart';
import 'core/storage/auth_storage.dart';
import 'core/storage/theme_storage.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/login_screen.dart';
import 'features/auth/totp_setup_screen.dart';
import 'features/auth/totp_verify_screen.dart';
import 'features/home/home_screen.dart';
import 'providers/auth_provider.dart';
import 'providers/cart_provider.dart';
import 'providers/theme_provider.dart';
import 'services/access_menu_service.dart';
import 'services/auth_service.dart';
import 'services/catalog_service.dart';
import 'services/discount_service.dart';
import 'services/product_service.dart';
import 'services/purchase_service.dart';
import 'services/sale_service.dart';

void main() {
  // Poppins viaja dentro del `.aab` (carpeta `google_fonts/`, declarada como
  // asset). Cortar la descarga en tiempo de ejecución evita una conexión a los
  // servidores de Google —que habría que declarar en el formulario de seguridad
  // de los datos de Play— y que la app arranque con la tipografía por defecto
  // donde la conexión es mala. En debug, una variante que falte revienta acá
  // en lugar de disimularse bajando el archivo.
  GoogleFonts.config.allowRuntimeFetching = false;

  final storage = AuthStorage();
  final api = ApiClient(storage);
  // Se crea acá y no dentro del árbol de widgets porque el AuthProvider
  // necesita una referencia para vaciarlo al cerrar sesión.
  final cart = CartProvider();

  runApp(InventoryApp(storage: storage, api: api, cart: cart));
}

class InventoryApp extends StatelessWidget {
  const InventoryApp({
    super.key,
    required this.storage,
    required this.api,
    required this.cart,
  });

  final AuthStorage storage;
  final ApiClient api;
  final CartProvider cart;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        // Servicios (sin estado) disponibles vía context.read.
        Provider.value(value: api),
        Provider(create: (_) => ProductService(api)),
        Provider(create: (_) => CatalogService(api)),
        Provider(create: (_) => SaleService(api)),
        Provider(create: (_) => DiscountService(api)),
        Provider(create: (_) => PurchaseService(api)),
        ChangeNotifierProvider(
          create: (_) =>
              AuthProvider(AuthService(api), storage, api, AccessMenuService(api))
                // El carrito pertenece al turno, no a la app: al cerrar sesión
                // no puede quedar para el cajero siguiente.
                ..onSessionEnd = cart.clear
                ..bootstrap(),
        ),
        ChangeNotifierProvider.value(value: cart),
        ChangeNotifierProvider(
          create: (_) => ThemeProvider(ThemeStorage())..load(),
        ),
      ],
      child: Consumer<ThemeProvider>(
        builder: (_, theme, __) => MaterialApp(
          title: AppConfig.appName,
          debugShowCheckedModeBanner: false,
          navigatorKey: navigatorKey,
          theme: AppTheme.light(),
          darkTheme: AppTheme.dark(),
          themeMode: theme.mode,
          home: const _Root(),
        ),
      ),
    );
  }
}

/// Decide la pantalla inicial según el estado de autenticación.
class _Root extends StatelessWidget {
  const _Root();

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    switch (auth.status) {
      case AuthStatus.unknown:
        return const Scaffold(body: Center(child: CircularProgressIndicator()));
      case AuthStatus.authenticated:
        return const HomeScreen();
      case AuthStatus.totpRequired:
        return const TotpVerifyScreen();
      case AuthStatus.totpSetupRequired:
        return const TotpSetupScreen();
      case AuthStatus.unauthenticated:
        return const LoginScreen();
    }
  }
}
