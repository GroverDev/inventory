/// Modelos de catálogos simples (id + nombre) usados en formularios y POS.
class NamedItem {
  final String id;
  final String name;

  NamedItem({required this.id, required this.name});

  /// Construye desde claves variables según el endpoint
  /// (CategoryName, LaboratoryName, UnitName, FullName, etc.).
  factory NamedItem.from(Map<String, dynamic> j, List<String> nameKeys) {
    String name = '';
    for (final k in nameKeys) {
      if (j[k] != null && j[k].toString().isNotEmpty) {
        name = j[k].toString();
        break;
      }
    }
    return NamedItem(id: (j['Id'] ?? '').toString(), name: name);
  }

  @override
  bool operator ==(Object other) => other is NamedItem && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class PaymentMethod {
  final String id;
  final String name;

  /// Clase de icono FontAwesome usada en la web (ej: "fal fa-money-bill").
  final String iconCss;

  /// Si el método entrega vuelto (efectivo). Tarjeta/QR normalmente no.
  final bool requiresChanges;

  /// Si el cobro entra al cajon. Un reintegro por este medio mueve la caja y
  /// exige tener una sesion de caja abierta.
  final bool affectsCash;

  PaymentMethod({
    required this.id,
    required this.name,
    this.iconCss = '',
    this.requiresChanges = false,
    this.affectsCash = false,
  });

  factory PaymentMethod.fromJson(Map<String, dynamic> j) => PaymentMethod(
        id: (j['Id'] ?? '').toString(),
        name: j['Name'] ?? j['PaymentMethodName'] ?? j['MethodName'] ?? '',
        iconCss: j['IconCss'] ?? '',
        requiresChanges: j['RequiresChanges'] ?? false,
        affectsCash: j['AffectsCash'] ?? false,
      );
}

/// Cliente para el POS (espejo de Customer del backend).
class Customer {
  final String id;
  final String fullName;
  final String documentNumber;

  Customer({
    required this.id,
    required this.fullName,
    required this.documentNumber,
  });

  factory Customer.fromJson(Map<String, dynamic> j) => Customer(
        id: (j['Id'] ?? '').toString(),
        fullName: j['FullName'] ?? j['CustomerName'] ?? j['Name'] ?? '',
        documentNumber: j['DocumentNumber'] ?? '',
      );
}

class PurchaseStatus {
  final int id;
  final String name;

  PurchaseStatus({required this.id, required this.name});

  factory PurchaseStatus.fromJson(Map<String, dynamic> j) => PurchaseStatus(
        id: j['Id'] ?? 0,
        name: j['Description'] ??
            j['Name'] ??
            j['StatusName'] ??
            j['PurchaseStatusName'] ??
            '',
      );
}
