import 'package:flutter/material.dart';

/// Permite navegar desde código sin [BuildContext] (ej. AuthProvider al
/// detectar un 401), cerrando cualquier pantalla apilada para volver a la
/// raíz, donde `_Root` decide qué mostrar según el estado de auth.
final navigatorKey = GlobalKey<NavigatorState>();
