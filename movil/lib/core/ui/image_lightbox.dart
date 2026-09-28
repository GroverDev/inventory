import 'dart:typed_data';

import 'package:flutter/material.dart';

/// Imagen entera a pantalla completa. Espejo de `ImageLightbox.vue`: fondo
/// oscuro, y se cierra tocando afuera o la X.
///
/// Pasa [src] (una URL) o [bytes] (una foto recién elegida, todavía sin
/// subir) — exactamente uno de los dos.
///
/// El fondo y la imagen van en `Positioned` separados a propósito: un
/// `GestureDetector` envolviendo un hijo que se estira a ocupar todo el alto
/// disponible (como `Image` dentro de un `Center` sin límites, o
/// `InteractiveViewer`) absorbe el toque en toda la pantalla, no solo sobre
/// la foto — "tocar afuera para cerrar" quedaría muerto.
Future<void> showImageLightbox(
  BuildContext context, {
  String? src,
  Uint8List? bytes,
  String? caption,
}) {
  assert((src != null) != (bytes != null), 'Pasa src o bytes, no los dos.');
  final image = bytes != null
      ? Image.memory(bytes, fit: BoxFit.contain, errorBuilder: _errorBuilder)
      : Image.network(src!, fit: BoxFit.contain, errorBuilder: _errorBuilder);

  return showDialog<void>(
    context: context,
    barrierColor: Colors.black87,
    builder: (dialogContext) => Dialog.fullscreen(
      backgroundColor: Colors.black,
      child: Stack(
        children: [
          Positioned.fill(
            child: GestureDetector(onTap: () => Navigator.pop(dialogContext)),
          ),
          Center(
            child: GestureDetector(
              // Absorbe el toque sobre la imagen misma: tocarla no debe
              // cerrar el visor, solo tocar el fondo.
              onTap: () {},
              child: Padding(padding: const EdgeInsets.all(16), child: image),
            ),
          ),
          Positioned(
            top: 8,
            right: 8,
            child: SafeArea(
              child: IconButton(
                tooltip: 'Cerrar',
                icon: const Icon(Icons.close, color: Colors.white),
                style: IconButton.styleFrom(backgroundColor: Colors.white24),
                onPressed: () => Navigator.pop(dialogContext),
              ),
            ),
          ),
          if (caption != null && caption.isNotEmpty)
            Positioned(
              bottom: 24,
              left: 16,
              right: 16,
              child: IgnorePointer(
                child: Text(
                  caption,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.white),
                ),
              ),
            ),
        ],
      ),
    ),
  );
}

Widget _errorBuilder(BuildContext context, Object error, StackTrace? stack) =>
    const Padding(
      padding: EdgeInsets.all(24),
      child: Text('No se pudo cargar la imagen.',
          style: TextStyle(color: Colors.white70)),
    );
