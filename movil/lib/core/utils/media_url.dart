import '../config/app_config.dart';

/// URL pública de una imagen de producto, o `''` si no hay imagen o no hay
/// dominio configurado (quien la usa muestra entonces el marcador).
///
/// [thumb] pide la miniatura (200 px) en vez de la imagen completa (800 px):
/// es la misma ruta con `_thumb` antes de la extensión. Para listados y el POS,
/// donde pesa una fracción. Misma regla que `mediaUrl()` de la web.
String mediaUrl(
  String? path, {
  bool thumb = false,
  String base = AppConfig.mediaBaseUrl,
}) {
  final b = base.trim();
  if (path == null || path.isEmpty || b.isEmpty) return '';
  final relative = thumb ? path.replaceFirst(RegExp(r'\.webp$', caseSensitive: false), '_thumb.webp') : path;
  return '${b.endsWith('/') ? b : '$b/'}${relative.replaceFirst(RegExp(r'^/+'), '')}';
}
