/// Espejo de ProductResponse / ProductRequest del backend.
class Product {
  final String id;
  String productCode;
  String productName;
  String description;
  double salePrice;
  String barCode;
  int currentStock;
  int minReorderQuantity;
  bool availableInPos;
  String uomId;
  String unitName;
  String laboratoryId;
  String laboratoryName;
  String categoryId;
  String categoryName;
  bool isActive;

  Product({
    this.id = '',
    this.productCode = '',
    this.productName = '',
    this.description = '',
    this.salePrice = 0,
    this.barCode = '',
    this.currentStock = 0,
    this.minReorderQuantity = 0,
    this.availableInPos = true,
    this.uomId = '',
    this.unitName = '',
    this.laboratoryId = '',
    this.laboratoryName = '',
    this.categoryId = '',
    this.categoryName = '',
    this.isActive = true,
  });

  factory Product.fromJson(Map<String, dynamic> j) => Product(
        id: (j['Id'] ?? '').toString(),
        productCode: j['ProductCode'] ?? '',
        productName: j['ProductName'] ?? '',
        description: j['Description'] ?? '',
        salePrice: (j['SalePrice'] ?? 0).toDouble(),
        barCode: j['BarCode'] ?? '',
        currentStock: j['CurrentStock'] ?? 0,
        minReorderQuantity: j['MinReorderQuantity'] ?? 0,
        availableInPos: j['AvailableInPos'] ?? false,
        uomId: (j['UomId'] ?? '').toString(),
        unitName: j['UnitName'] ?? '',
        laboratoryId: (j['LaboratoryId'] ?? '').toString(),
        laboratoryName: j['LaboratoryName'] ?? '',
        categoryId: (j['CategoryId'] ?? '').toString(),
        categoryName: j['CategoryName'] ?? '',
        isActive: j['IsActive'] ?? false,
      );

  /// Payload para POST/PUT api/Product (espejo de ProductRequest).
  Map<String, dynamic> toRequest() => {
        'Id': id,
        'ProductCode': productCode,
        'ProductName': productName,
        'Description': description,
        'SalePrice': salePrice,
        'UomId': uomId,
        'CurrentStock': currentStock,
        'IsActive': isActive,
        'MinReorderQuantity': minReorderQuantity,
        'AvailableInPos': availableInPos,
        'LaboratoryId': laboratoryId,
        'CategoryId': categoryId,
        'BarCode': barCode,
      };

  bool get lowStock => currentStock <= minReorderQuantity;
}
