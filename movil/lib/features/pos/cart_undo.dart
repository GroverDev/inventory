import 'package:flutter/material.dart';

import '../../providers/cart_provider.dart';

/// Aviso de que se quitó un producto, con una ventana para deshacerlo.
/// Espejo del botón "Deshacer" que aparece en el punto de venta web (6 s,
/// `removeWithUndo`/`undoRemove` en `PointOfSaleView.vue` y en
/// `CartProvider`).
void showUndoSnack(BuildContext context, CartProvider cart) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      content: const Text('Producto quitado.'),
      duration: const Duration(seconds: 6),
      action: SnackBarAction(label: 'Deshacer', onPressed: cart.undoRemove),
    ));
}
