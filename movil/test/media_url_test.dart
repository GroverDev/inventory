import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/utils/media_url.dart';
import 'package:inventory_movil/models/product.dart';

/// Misma regla que `mediaUrl()` de la web: la API manda solo la ruta relativa y
/// cada cliente la une a su dominio de medios.
void main() {
  const base = 'https://media.example.com/';
  const path = 'abc123/products/p1/f00.webp';

  group('mediaUrl', () {
    test('une la base con la ruta relativa', () {
      expect(mediaUrl(path, base: base), 'https://media.example.com/abc123/products/p1/f00.webp');
    });

    test('la miniatura es la misma ruta con _thumb antes de la extensión', () {
      expect(mediaUrl(path, thumb: true, base: base),
          'https://media.example.com/abc123/products/p1/f00_thumb.webp');
    });

    test('tolera una base sin barra final y una ruta con barra inicial', () {
      expect(mediaUrl('/$path', base: 'https://media.example.com'),
          'https://media.example.com/abc123/products/p1/f00.webp');
    });

    test('sin imagen o sin dominio devuelve vacío para mostrar el marcador', () {
      expect(mediaUrl(null, base: base), '');
      expect(mediaUrl('', base: base), '');
      expect(mediaUrl(path, base: ''), '');
    });
  });

  group('Product.imagePath', () {
    test('se lee de ImagePath y es null si el producto no tiene', () {
      expect(Product.fromJson({'Id': '1', 'ImagePath': path}).imagePath, path);
      expect(Product.fromJson({'Id': '1'}).imagePath, isNull);
      expect(Product.fromJson({'Id': '1', 'ImagePath': null}).imagePath, isNull);
    });

    test('no viaja en el guardado de la ficha: la fija el servidor al subirla', () {
      expect(Product(id: '1', imagePath: path).toRequest().containsKey('ImagePath'), isFalse);
    });
  });
}
