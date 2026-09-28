import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../core/network/api_response.dart';
import '../../core/ui/confirm_dialog.dart';
import '../../core/ui/image_lightbox.dart';
import '../../core/utils/media_url.dart';
import '../../services/product_service.dart';

const List<String> _allowedExtensions = ['jpg', 'jpeg', 'png', 'webp'];
const int _maxImageBytes = 5 * 1024 * 1024; // mismo tope que el servidor.

/// Foto del producto: elegir de la galería, tomar una con la cámara, ver la
/// actual en grande, o quitarla. Espejo de `ProductImageUploader.vue`.
///
/// Con el producto todavía sin crear ([productId] vacío) no hay a qué subirla
/// todavía: la foto elegida se guarda en memoria y [onPicked] avisa al
/// formulario para que la suba recién cuando el producto exista.
class ProductImageField extends StatefulWidget {
  const ProductImageField({
    super.key,
    required this.productId,
    required this.imagePath,
    required this.readOnly,
    required this.onUploaded,
    required this.onRemoved,
    required this.onPicked,
  });

  final String productId;
  final String? imagePath;
  final bool readOnly;

  /// Producto ya existente: la imagen se subió y esta es su nueva ruta.
  final void Function(String path) onUploaded;
  final VoidCallback onRemoved;

  /// Producto nuevo: el archivo elegido (o `null` al quitarlo), para que el
  /// formulario lo suba tras crear el producto.
  final void Function(Uint8List? bytes, String name) onPicked;

  @override
  State<ProductImageField> createState() => _ProductImageFieldState();
}

class _ProductImageFieldState extends State<ProductImageField> {
  Uint8List? _pickedBytes;
  String _pickedName = '';
  bool _busy = false;
  String? _error;

  bool get _isNew => widget.productId.isEmpty;
  bool get _hasImage => widget.imagePath != null || _pickedBytes != null;

  Future<void> _pick(ImageSource source) async {
    setState(() => _error = null);
    final XFile? file;
    try {
      file = await ImagePicker().pickImage(source: source, imageQuality: 90);
    } catch (_) {
      setState(() => _error = source == ImageSource.camera
          ? 'No se pudo acceder a la cámara.'
          : 'No se pudo abrir la galería.');
      return;
    }
    if (file == null) return; // el usuario canceló

    final ext = file.name.split('.').last.toLowerCase();
    if (!_allowedExtensions.contains(ext)) {
      setState(() => _error = 'Formato no permitido. Use JPG, PNG o WebP.');
      return;
    }
    final bytes = await file.readAsBytes();
    if (bytes.lengthInBytes > _maxImageBytes) {
      setState(() => _error = 'La imagen no puede pesar más de 5 MB.');
      return;
    }

    if (_isNew) {
      setState(() {
        _pickedBytes = bytes;
        _pickedName = file!.name;
      });
      widget.onPicked(bytes, file.name);
      return;
    }

    setState(() => _busy = true);
    try {
      final path = await context
          .read<ProductService>()
          .uploadImage(widget.productId, bytes, file.name);
      widget.onUploaded(path);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _remove() async {
    if (_isNew) {
      setState(() {
        _pickedBytes = null;
        _pickedName = '';
      });
      widget.onPicked(null, '');
      return;
    }
    final ok = await confirm(
      context,
      title: 'Quitar imagen',
      message: '¿Desea quitar la imagen del producto?',
      confirmLabel: 'Quitar',
      destructive: true,
    );
    if (!ok) return;
    setState(() => _busy = true);
    try {
      await context.read<ProductService>().deleteImage(widget.productId);
      widget.onRemoved();
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _openLightbox() {
    if (_pickedBytes != null) {
      showImageLightbox(context, bytes: _pickedBytes);
    } else if (widget.imagePath != null) {
      showImageLightbox(context, src: mediaUrl(widget.imagePath));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            borderRadius: BorderRadius.circular(8),
            onTap: _hasImage ? _openLightbox : null,
            child: Container(
              width: 110,
              height: 110,
              clipBehavior: Clip.antiAlias,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(8),
                border: Border.all(
                    color: Theme.of(context).colorScheme.outlineVariant),
              ),
              child: _pickedBytes != null
                  ? Image.memory(_pickedBytes!, fit: BoxFit.cover)
                  : (mediaUrl(widget.imagePath, thumb: true).isNotEmpty
                      ? Image.network(
                          mediaUrl(widget.imagePath, thumb: true),
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => const _NoImage(),
                        )
                      : const _NoImage()),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (!widget.readOnly)
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      OutlinedButton.icon(
                        onPressed: _busy ? null : () => _pick(ImageSource.gallery),
                        icon: const Icon(Icons.photo_library_outlined, size: 18),
                        label: Text(_hasImage ? 'Cambiar imagen' : 'Subir imagen'),
                      ),
                      OutlinedButton.icon(
                        onPressed: _busy ? null : () => _pick(ImageSource.camera),
                        icon: const Icon(Icons.camera_alt_outlined, size: 18),
                        label: const Text('Tomar foto'),
                      ),
                      if (_hasImage)
                        OutlinedButton.icon(
                          onPressed: _busy ? null : _remove,
                          style: OutlinedButton.styleFrom(foregroundColor: Colors.red),
                          icon: const Icon(Icons.delete_outline, size: 18),
                          label: const Text('Quitar'),
                        ),
                      if (_busy)
                        const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2)),
                    ],
                  ),
                const SizedBox(height: 4),
                const Text('JPG, PNG o WebP. Máximo 5 MB.',
                    style: TextStyle(fontSize: 12, color: Colors.grey)),
                if (_isNew && _pickedName.isNotEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text('«$_pickedName» se subirá al guardar el producto.',
                        style: const TextStyle(fontSize: 12, color: Colors.grey)),
                  )
                else if (_isNew)
                  const Padding(
                    padding: EdgeInsets.only(top: 2),
                    child: Text('Puede elegirla ahora; se sube al guardar el producto.',
                        style: TextStyle(fontSize: 12, color: Colors.grey)),
                  ),
                if (_error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(_error!,
                        style: const TextStyle(fontSize: 12, color: Colors.red)),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _NoImage extends StatelessWidget {
  const _NoImage();

  @override
  Widget build(BuildContext context) => Container(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        child: Icon(Icons.image_outlined,
            color: Theme.of(context).colorScheme.onSurfaceVariant),
      );
}
