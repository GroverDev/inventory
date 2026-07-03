/// Espejo de Discount del backend (catálogo de descuentos predefinidos).
class Discount {
  final String id;
  final String name;
  final String description;

  /// 'Percentage' | 'FixedAmount'
  final String type;
  final double value;
  final bool isActive;

  Discount({
    required this.id,
    required this.name,
    required this.description,
    required this.type,
    required this.value,
    required this.isActive,
  });

  bool get isPercentage => type == 'Percentage';

  /// Etiqueta corta del valor (ej: "10%" o "Bs. 5.00").
  String get valueLabel =>
      isPercentage ? '${_trim(value)}%' : 'Bs. ${value.toStringAsFixed(2)}';

  factory Discount.fromJson(Map<String, dynamic> j) => Discount(
        id: (j['Id'] ?? '').toString(),
        name: j['Name'] ?? '',
        description: j['Description'] ?? '',
        type: j['Type'] ?? '',
        value: (j['Value'] ?? 0).toDouble(),
        isActive: j['IsActive'] ?? false,
      );

  static String _trim(double v) =>
      v == v.roundToDouble() ? v.toInt().toString() : v.toString();
}
