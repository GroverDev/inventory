import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/models/product.dart';
import 'package:inventory_movil/providers/cart_provider.dart';

/// Cubre lo que sumó la Fase 4 al carrito: deshacer al quitar una línea y
/// las líneas de productos serializados, que no se ajustan con +/- sueltos.
void main() {
  final normal = Product(id: 'p1', productName: 'Ibuprofeno', salePrice: 10, currentStock: 20);
  final serial =
      Product(id: 'p2', productName: 'Termómetro', salePrice: 50, currentStock: 5, trackingMode: 'serial');

  group('deshacer al quitar', () {
    test('quitar una línea y deshacer la devuelve tal como estaba', () {
      final cart = CartProvider()..add(normal)..add(normal); // quantity 2

      cart.remove(cart.lines.first);
      expect(cart.isEmpty, isTrue);

      cart.undoRemove();
      expect(cart.lines, hasLength(1));
      expect(cart.lines.first.quantity, 2);
    });

    test('bajar de 1 a 0 con el "-" también se puede deshacer', () {
      final cart = CartProvider()..add(normal); // quantity 1

      cart.decrement(cart.lines.first);
      expect(cart.isEmpty, isTrue);

      cart.undoRemove();
      expect(cart.lines, hasLength(1));
      expect(cart.lines.first.quantity, 1); // no quedó en 0
    });

    test('si mientras tanto se volvió a agregar el mismo producto, deshacer no duplica', () {
      final cart = CartProvider()..add(normal);
      cart.remove(cart.lines.first);
      cart.add(normal); // el cajero lo volvió a tocar

      cart.undoRemove();

      expect(cart.lines, hasLength(1)); // no dos líneas del mismo producto
    });

    test('deshacer sin nada que deshacer no rompe nada', () {
      final cart = CartProvider();
      expect(() => cart.undoRemove(), returnsNormally);
      expect(cart.isEmpty, isTrue);
    });

    test('lastRemoved refleja la última línea quitada, para que la UI decida si avisar', () {
      final cart = CartProvider()..add(normal);
      expect(cart.lastRemoved, isNull);

      cart.remove(cart.lines.first);
      expect(cart.lastRemoved?.product.id, 'p1');

      cart.undoRemove();
      expect(cart.lastRemoved, isNull);
    });
  });

  group('productos serializados', () {
    test('setSerializedLine agrega la línea con la cantidad de series elegidas', () {
      final cart = CartProvider();
      cart.setSerializedLine(serial, ['SN-1', 'SN-2']);

      expect(cart.lines, hasLength(1));
      expect(cart.lines.first.quantity, 2);
      expect(cart.lines.first.serialNumbers, ['SN-1', 'SN-2']);
    });

    test('volver a elegir series reemplaza la selección anterior, no la acumula', () {
      final cart = CartProvider();
      cart.setSerializedLine(serial, ['SN-1', 'SN-2']);
      cart.setSerializedLine(serial, ['SN-3']);

      expect(cart.lines, hasLength(1));
      expect(cart.lines.first.quantity, 1);
      expect(cart.lines.first.serialNumbers, ['SN-3']);
    });

    test('elegir cero series quita la línea del carrito', () {
      final cart = CartProvider();
      cart.setSerializedLine(serial, ['SN-1']);
      cart.setSerializedLine(serial, []);

      expect(cart.isEmpty, isTrue);
    });

    test('add() ignora un producto serializado: no suma sin número de serie', () {
      final cart = CartProvider();
      cart.add(serial);
      expect(cart.isEmpty, isTrue);
    });

    test('increment/decrement no tocan una línea serializada: hay que reabrir el selector', () {
      final cart = CartProvider();
      cart.setSerializedLine(serial, ['SN-1', 'SN-2']);
      final line = cart.lines.first;

      cart.increment(line);
      expect(line.quantity, 2); // sin cambio

      cart.decrement(line);
      expect(line.quantity, 2); // sin cambio, y no se quitó tampoco
      expect(cart.isEmpty, isFalse);
    });
  });

  group('SaleLine.toJson', () {
    test('incluye SerialNumbers, vacío para un producto normal', () {
      final cart = CartProvider()..add(normal);
      expect(cart.lines.first.toJson()['SerialNumbers'], isEmpty);
    });

    test('con series elegidas, viajan en el payload', () {
      final cart = CartProvider();
      cart.setSerializedLine(serial, ['SN-9']);
      expect(cart.lines.first.toJson()['SerialNumbers'], ['SN-9']);
    });
  });
}
