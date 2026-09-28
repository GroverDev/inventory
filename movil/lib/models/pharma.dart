/// Un componente del producto con su concentración (parte de [ProductPharma]).
class ProductComponent {
  final String substanceName;
  final double? concentrationValue;
  final String concentrationUnit;

  /// `false` para los excipientes: responden otra pregunta que la
  /// composición ("¿puedo tomarlo?" — gluten, lactosa, azúcar — no "¿qué es?").
  final bool isActiveIngredient;

  ProductComponent({
    this.substanceName = '',
    this.concentrationValue,
    this.concentrationUnit = '',
    this.isActiveIngredient = true,
  });

  factory ProductComponent.fromJson(Map<String, dynamic> j) => ProductComponent(
        substanceName: j['SubstanceName'] ?? '',
        concentrationValue: (j['ConcentrationValue'] as num?)?.toDouble(),
        concentrationUnit: j['ConcentrationUnit'] ?? '',
        isActiveIngredient: j['IsActiveIngredient'] ?? true,
      );
}

/// Ficha farmacéutica del producto (GET api/Pharma/product/{id}).
class ProductPharma {
  final String formName;
  final String routeName;
  final String presentation;

  /// Del prospecto. No es una recomendación del sistema: la dosis la indica
  /// quien receta.
  final String dosageReference;
  final List<ProductComponent> components;

  ProductPharma({
    this.formName = '',
    this.routeName = '',
    this.presentation = '',
    this.dosageReference = '',
    this.components = const [],
  });

  factory ProductPharma.fromJson(Map<String, dynamic> j) => ProductPharma(
        formName: j['FormName'] ?? '',
        routeName: j['RouteName'] ?? '',
        presentation: j['Presentation'] ?? '',
        dosageReference: j['DosageReference'] ?? '',
        components: ((j['Components'] as List?) ?? [])
            .map((e) => ProductComponent.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  bool get hasData =>
      components.isNotEmpty || formName.isNotEmpty || dosageReference.isNotEmpty;
}

/// Un producto que puede ofrecerse en lugar de otro
/// (GET api/Pharma/product/{id}/equivalents).
///
/// Van en dos grupos: los deducidos por composición (`isManual: false`, de
/// verdad intercambiables) y los que la farmacia definió a mano
/// (`isManual: true`, pueden tener otro principio activo — es una sugerencia,
/// no una equivalencia clínica).
class ProductEquivalent {
  final String productId;
  final String productName;
  final double salePrice;
  final int currentStock;
  final String productType;
  final String presentation;
  final bool isManual;
  final String reason;

  ProductEquivalent({
    required this.productId,
    required this.productName,
    required this.salePrice,
    required this.currentStock,
    this.productType = '',
    this.presentation = '',
    this.isManual = false,
    this.reason = '',
  });

  factory ProductEquivalent.fromJson(Map<String, dynamic> j) => ProductEquivalent(
        productId: (j['ProductId'] ?? '').toString(),
        productName: j['ProductName'] ?? '',
        salePrice: (j['SalePrice'] ?? 0).toDouble(),
        currentStock: (j['CurrentStock'] as num? ?? 0).toInt(),
        productType: j['ProductType'] ?? '',
        presentation: j['Presentation'] ?? '',
        isManual: j['IsManual'] ?? false,
        reason: j['Reason'] ?? '',
      );

  /// 'Genérico' | 'Marca' | 'Similar', o el valor crudo si no se reconoce.
  String get typeLabel => switch (productType) {
        'generico' => 'Genérico',
        'marca' => 'Marca',
        'similar' => 'Similar',
        _ => productType,
      };
}
