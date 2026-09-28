/// Una unidad serializada disponible para vender
/// (GET api/StockMovement/serials/{productId}).
class StockSerial {
  final String stockItemId;
  final String serialNumber;
  final DateTime? expiryDate;

  StockSerial({
    required this.stockItemId,
    required this.serialNumber,
    this.expiryDate,
  });

  factory StockSerial.fromJson(Map<String, dynamic> j) => StockSerial(
        stockItemId: (j['StockItemId'] ?? '').toString(),
        serialNumber: j['SerialNumber'] ?? '',
        expiryDate: j['ExpiryDate'] == null
            ? null
            : DateTime.tryParse(j['ExpiryDate'].toString()),
      );
}
