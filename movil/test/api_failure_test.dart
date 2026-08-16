import 'package:flutter_test/flutter_test.dart';
import 'package:inventory_movil/core/network/api_response.dart';

/// La regla que decide si el usuario se entera de que algo falló.
///
/// Antes solo se lanzaba ante `MessageType: error`, y como el backend responde
/// sus reglas de negocio con `warning`, un rechazo volvía como éxito silencioso:
/// la pantalla seguía como si nada y el usuario creía que su acción se guardó.
void main() {
  group('apiFailure', () {
    test('una respuesta correcta no es falla', () {
      expect(apiFailure(true, ApiMessage(), null), isNull);
    });

    test('un rechazo de negocio (warning) es falla', () {
      final falla = apiFailure(
        false,
        ApiMessage(description: 'El pendiente es 10.', messageType: 'warning'),
        null,
      );

      expect(falla, isNotNull);
      expect(falla!.message, 'El pendiente es 10.');
      expect(falla.messageType, 'warning');
    });

    test('un info también es falla, pero se puede distinguir', () {
      // El servicio de compras lo usa para no tratar como error el reintento
      // de una recepción que ya había entrado.
      final falla = apiFailure(
        false,
        ApiMessage(description: 'Ya fue registrada.', messageType: 'info'),
        null,
      );

      expect(falla!.isInfo, isTrue);
    });

    test('sin descripción se recurre al cuerpo crudo', () {
      // Es el caso de un 400 que no viene envuelto en Response<T>.
      final falla = apiFailure(false, ApiMessage(), {
        'title': 'One or more validation errors occurred.',
        'errors': {
          'AdminPassword': ['La contraseña debe tener al menos 8 caracteres.']
        },
      });

      expect(falla!.message, 'La contraseña debe tener al menos 8 caracteres.');
    });
  });

  group('extractApiError', () {
    test('prefiere el mensaje del sobre del backend', () {
      expect(
        extractApiError({
          'Message': {'Description': 'No hay stock suficiente.'}
        }),
        'No hay stock suficiente.',
      );
    });

    test('lee el ProblemDetails de la validación de ASP.NET', () {
      // Sin esto el usuario solo veía "Ocurrió un error inesperado" ante
      // cualquier error de validación del modelo.
      expect(
        extractApiError({
          'title': 'One or more validation errors occurred.',
          'errors': {
            'Email': ['Formato de Correo Electrónico incorrecto.']
          },
        }),
        'Formato de Correo Electrónico incorrecto.',
      );
    });

    test('cae al title cuando no hay detalle por campo', () {
      expect(extractApiError({'title': 'Bad Request'}), 'Bad Request');
    });

    test('un texto plano se usa tal cual', () {
      expect(extractApiError('Servicio no disponible'), 'Servicio no disponible');
    });

    test('un cuerpo que no dice nada devuelve un texto entendible', () {
      expect(extractApiError(null), 'Ocurrió un error inesperado.');
      expect(extractApiError({}), 'Ocurrió un error inesperado.');
    });
  });
}
