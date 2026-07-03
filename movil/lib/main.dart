import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'core/config/app_config.dart';
import 'core/network/api_client.dart';
import 'core/storage/auth_storage.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/login_screen.dart';
import 'features/auth/totp_setup_screen.dart';
import 'features/auth/totp_verify_screen.dart';
import 'features/home/home_screen.dart';
import 'providers/auth_provider.dart';
import 'providers/cart_provider.dart';
import 'services/auth_service.dart';
import 'services/catalog_service.dart';
import 'services/discount_service.dart';
import 'services/product_service.dart';
import 'services/purchase_service.dart';
import 'services/sale_service.dart';

void main() {
  final storage = AuthStorage();
  final api = ApiClient(storage);

  runApp(InventoryApp(storage: storage, api: api));
}

class InventoryApp extends StatelessWidget {
  const InventoryApp({super.key, required this.storage, required this.api});

  final AuthStorage storage;
  final ApiClient api;

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
          create: (_) => AuthProvider(AuthService(api), storage, api)..bootstrap(),
        ),
        ChangeNotifierProvider(create: (_) => CartProvider()),
      ],
      child: MaterialApp(
        title: AppConfig.appName,
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light(),
        darkTheme: AppTheme.dark(),
        home: const _Root(),
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
