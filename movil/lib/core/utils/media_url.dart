import '../config/app_config.dart';

/// URL pública de una imagen de producto. Espejo de
/// `frontend/src/utils/mediaUrl.ts`: la API solo devuelve la ruta relativa
/// (`{public_id}/products/{id}/{guid}.webp`), el dominio lo pone cada
/// cliente ([AppConfig.mediaBaseUrl]).
///
/// Devuelve `''` si no hay imagen o no hay dominio configurado; quien la usa
/// muestra entonces el marcador.
///
/// [thumb]: miniatura (200 px) en vez de la imagen completa (800 px). Para
/// listados y la grilla del POS: pesa una fracción y carga al instante.
String mediaUrl(String? path, {bool thumb = false}) {
  if (path == null || path.isEmpty || AppConfig.mediaBaseUrl.isEmpty) return '';
  final base = AppConfig.mediaBaseUrl.endsWith('/')
      ? AppConfig.mediaBaseUrl
      : '${AppConfig.mediaBaseUrl}/';
  final relative =
      thumb ? path.replaceFirst(RegExp(r'\.webp$', caseSensitive: false), '_thumb.webp') : path;
  return base + relative.replaceFirst(RegExp(r'^/+'), '');
}
