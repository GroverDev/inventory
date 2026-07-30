import 'package:flutter/material.dart';

import '../core/storage/theme_storage.dart';

/// Mantiene el modo de tema elegido y lo persiste.
class ThemeProvider extends ChangeNotifier {
  ThemeProvider(this._storage);

  final ThemeStorage _storage;

  ThemeMode mode = ThemeMode.system;

  Future<void> load() async {
    mode = await _storage.read();
    notifyListeners();
  }

  Future<void> setMode(ThemeMode value) async {
    if (value == mode) return;
    mode = value;
    notifyListeners();
    await _storage.save(value);
  }
}
