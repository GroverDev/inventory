import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../core/utils/media_url.dart';
import '../../services/product_service.dart';

/// Miniatura de un producto, cuadrada. Sin imagen (o si no carga) muestra el
/// marcador: la tarjeta queda igual que antes y no gasta espacio en un cuadro
/// vacío distinto.
///
/// [full] pide la imagen completa (800 px) en vez de la miniatura (200 px); para
/// la ficha, no para listados.
class ProductThumb extends StatelessWidget {
  const ProductThumb({
    super.key,
    required this.path,
    this.size = 48,
    this.full = false,
  });

  final String? path;
  final double size;
  final bool full;

  @override
  Widget build(BuildContext context) {
    final url = mediaUrl(path, thumb: !full);
    final placeholder = _Placeholder(size: size);
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: SizedBox(
        width: size,
        height: size,
        child: url.isEmpty
            ? placeholder
            : Image.network(
                url,
                fit: BoxFit.cover,
                // Decodifica al tamaño en que se dibuja: en una grilla de
                // productos la diferencia de memoria es grande.
                cacheWidth: (size * MediaQuery.devicePixelRatioOf(context)).round(),
                errorBuilder: (_, __, ___) => placeholder,
                loadingBuilder: (_, child, progress) => progress == null ? child : placeholder,
              ),
      ),
    );
  }
}

class _Placeholder extends StatelessWidget {
  const _Placeholder({required this.size});
  final double size;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ColoredBox(
      color: scheme.surfaceContainerHighest,
      child: Icon(Icons.medication_outlined, size: size * 0.5, color: scheme.primary),
    );
  }
}

/// Imagen ampliada, con zoom. Muestra la miniatura mientras baja la completa.
Future<void> showProductImage(BuildContext context, String path) {
  return showDialog<void>(
    context: context,
    builder: (ctx) => Dialog(
      insetPadding: const EdgeInsets.all(12),
      child: Stack(
        alignment: Alignment.topRight,
        children: [
          InteractiveViewer(
            child: Image.network(
              mediaUrl(path),
              fit: BoxFit.contain,
              errorBuilder: (_, __, ___) => const Padding(
                padding: EdgeInsets.all(32),
                child: Text('No se pudo cargar la imagen.'),
              ),
              loadingBuilder: (_, child, progress) => progress == null
                  ? child
                  : Image.network(mediaUrl(path, thumb: true), fit: BoxFit.contain),
            ),
          ),
          IconButton(
            tooltip: 'Cerrar',
            icon: const Icon(Icons.close),
            onPressed: () => Navigator.pop(ctx),
          ),
        ],
      ),
    ),
  );
}

/// Campo de imagen de la ficha del producto.
///
/// Con el producto ya creado, la imagen se sube apenas se elige. En un alta
/// todavía no hay producto al que colgarla: se guarda el archivo elegido y el
/// padre lo sube cuando el producto exista ([onPicked]).
class ProductImageField extends StatefulWidget {
  const ProductImageField({
    super.key,
    required this.productId,
    required this.imagePath,
    this.readOnly = false,
    required this.onUploaded,
    required this.onRemoved,
    required this.onPicked,
  });

  /// Vacío mientras el producto no se ha creado.
  final String productId;
  final String? imagePath;
  final bool readOnly;

  /// Con producto ya creado: la imagen se subió y esta es su nueva ruta.
  final ValueChanged<String> onUploaded;
  final VoidCallback onRemoved;

  /// Con producto nuevo: el archivo elegido (o null si lo quitó).
  final void Function(String? filePath, String name) onPicked;

  @override
  State<ProductImageField> createState() => _ProductImageFieldState();
}

class _ProductImageFieldState extends State<ProductImageField> {
  final _picker = ImagePicker();
  bool _busy = false;
  String? _error;

  /// Archivo elegido en un alta, aún sin subir.
  String? _pendingPath;

  bool get _isNew => widget.productId.isEmpty;
  bool get _hasImage => _pendingPath != null || (widget.imagePath?.isNotEmpty ?? false);

  Future<void> _pick(ImageSource source) async {
    setState(() => _error = null);
    final XFile? file;
    try {
      // Las fotos del celular pesan 5-10 MB y el servidor las reduce a 800 px de
      // todos modos: se bajan acá y se ahorra la subida.
      file = await _picker.pickImage(
        source: source,
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 85,
      );
    } catch (_) {
      if (mounted) setState(() => _error = 'No se pudo abrir la ${source == ImageSource.camera ? 'cámara' : 'galería'}.');
      return;
    }
    if (file == null || !mounted) return;

    if (_isNew) {
      setState(() => _pendingPath = file!.path);
      widget.onPicked(file.path, file.name);
      return;
    }

    setState(() => _busy = true);
    try {
      final path = await context.read<ProductService>().uploadImage(widget.productId, file.path, file.name);
      if (!mounted) return;
      widget.onUploaded(path);
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _remove() async {
    if (_isNew) {
      setState(() => _pendingPath = null);
      widget.onPicked(null, '');
      return;
    }
    final service = context.read<ProductService>();
    final ok = await confirm(
      context,
      title: 'Quitar imagen',
      message: '¿Quitar la imagen del producto?',
      confirmLabel: 'Quitar',
      destructive: true,
    );
    if (!ok || !mounted) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await service.deleteImage(widget.productId);
      if (mounted) widget.onRemoved();
    } on ApiException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    const size = 110.0;
    final path = widget.imagePath;
    final Widget preview;
    if (_pendingPath != null) {
      preview = ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: Image.file(File(_pendingPath!), width: size, height: size, fit: BoxFit.cover),
      );
    } else {
      preview = ProductThumb(path: path, size: size, full: true);
    }

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        GestureDetector(
          onTap: (path != null && path.isNotEmpty && _pendingPath == null)
              ? () => showProductImage(context, path)
              : null,
          child: Stack(
            alignment: Alignment.center,
            children: [
              preview,
              if (_busy) const CircularProgressIndicator(),
            ],
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (!widget.readOnly)
                Wrap(
                  spacing: 8,
                  runSpacing: 4,
                  children: [
                    OutlinedButton.icon(
                      onPressed: _busy ? null : () => _pick(ImageSource.gallery),
                      icon: const Icon(Icons.photo_library_outlined, size: 18),
                      label: Text(_hasImage ? 'Cambiar' : 'Galería'),
                    ),
                    OutlinedButton.icon(
                      onPressed: _busy ? null : () => _pick(ImageSource.camera),
                      icon: const Icon(Icons.photo_camera_outlined, size: 18),
                      label: const Text('Cámara'),
                    ),
                    if (_hasImage)
                      TextButton.icon(
                        onPressed: _busy ? null : _remove,
                        icon: const Icon(Icons.delete_outline, size: 18),
                        label: const Text('Quitar'),
                        style: TextButton.styleFrom(foregroundColor: Colors.red),
                      ),
                  ],
                ),
              const SizedBox(height: 6),
              Text(
                _isNew && _pendingPath != null
                    ? 'Se subirá al guardar el producto.'
                    : 'Se reduce automáticamente; máximo 5 MB.',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(_error!, style: const TextStyle(color: Colors.red, fontSize: 12)),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
