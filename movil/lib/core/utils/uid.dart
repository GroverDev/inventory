import 'dart:math';

/// Genera un UUID v4 en el formato canónico `8-4-4-4-12`.
///
/// El backend lo lee con `Guid.TryParse`: si el texto no parsea, genera uno
/// propio y la operación **deja de ser idempotente** en silencio
/// (`PurchaseApplication.ReceiveOrders`). Por eso importa el formato exacto,
/// incluidos los bits de versión (4) y variante (10xx) que fija el RFC 4122.
String newUid() {
  final rnd = Random.secure();
  final bytes = List<int>.generate(16, (_) => rnd.nextInt(256));

  bytes[6] = (bytes[6] & 0x0f) | 0x40; // versión 4
  bytes[8] = (bytes[8] & 0x3f) | 0x80; // variante RFC 4122

  final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
  return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-'
      '${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
}
