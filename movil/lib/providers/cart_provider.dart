import 'dart:math' as math;

import 'package:flutter/foundation.dart';

import '../models/product.dart';
import '../models/sale.dart';

double _round(double v) => double.parse(v.toStringAsFixed(2));

/// Carrito del punto de venta (líneas + descuentos por línea y global).
class CartProvider extends ChangeNotifier {
  final List<SaleLine> _lines = [];

  // ── Descuento global (cabecera) ──────────────────────────
  String _headerDiscountId = '';
  String _headerDiscountLabel = '';
  String _headerDiscountType = '';
  double _headerDiscountValue = 0;

  List<SaleLine> get lines => List.unmodifiable(_lines);
  bool get isEmpty => _lines.isEmpty;
  int get itemCount => _lines.fold(0, (s, l) => s + l.quantity);

  // ── Totales ──────────────────────────────────────────────
  double get subtotal => _round(_lines.fold(0.0, (s, l) => s + l.lineSubtotal));
  double get totalLineDiscounts =>
      _round(_lines.fold(0.0, (s, l) => s + l.lineTotalDiscounts));

  /// Base sobre la que se calcula el descuento global.
  double get headerBase => _round(subtotal - totalLineDiscounts);

  double get headerDiscountAmount {
    if (_headerDiscountValue <= 0) return 0;
    final base = headerBase;
    if (_headerDiscountType == 'Percentage') {
      return _round(math.min(base * _headerDiscountValue / 100, base));
    }
    if (_headerDiscountType == 'FixedAmount') {
      return _round(math.min(_headerDiscountValue, base));
    }
    return 0;
  }

  double get totalDiscounts => _round(totalLineDiscounts + headerDiscountAmount);
  double get total => _round(subtotal - totalDiscounts);

  // ── Getters de descuento global ──────────────────────────
  String get headerDiscountId => _headerDiscountId;
  String get headerDiscountLabel => _headerDiscountLabel;
  String get headerDiscountType => _headerDiscountType;
  double get headerDiscountValue => _headerDiscountValue;
  bool get hasHeaderDiscount => headerDiscountAmount > 0;

  // ── Líneas ───────────────────────────────────────────────
  SaleLine? _find(String productId) {
    for (final l in _lines) {
      if (l.product.id == productId) return l;
    }
    return null;
  }

  int quantityOf(String productId) => _find(productId)?.quantity ?? 0;

  void add(Product product) {
    final existing = _find(product.id);
    if (existing != null) {
      if (product.currentStock <= 0 || existing.quantity < product.currentStock) {
        existing.quantity++;
      }
    } else {
      _lines.add(SaleLine(product: product, quantity: 1));
    }
    notifyListeners();
  }

  void increment(SaleLine line) {
    line.quantity++;
    notifyListeners();
  }

  void decrement(SaleLine line) {
    line.quantity--;
    if (line.quantity <= 0) _lines.remove(line);
    notifyListeners();
  }

  void decrementByProduct(String productId) {
    final line = _find(productId);
    if (line != null) decrement(line);
  }

  void remove(SaleLine line) {
    _lines.remove(line);
    notifyListeners();
  }

  // ── Descuentos de línea ──────────────────────────────────
  void setLineDiscount(
    SaleLine line, {
    required String type,
    required double value,
    String id = '',
    String label = '',
  }) {
    line.discountId = id;
    line.discountLabel = label;
    line.discountType = type;
    line.discountValue = value;
    notifyListeners();
  }

  void clearLineDiscount(SaleLine line) {
    line.clearDiscount();
    notifyListeners();
  }

  // ── Descuento global ─────────────────────────────────────
  void setHeaderDiscount({
    required String type,
    required double value,
    String id = '',
    String label = '',
  }) {
    _headerDiscountId = id;
    _headerDiscountLabel = label;
    _headerDiscountType = type;
    _headerDiscountValue = value;
    notifyListeners();
  }

  void clearHeaderDiscount() {
    _headerDiscountId = '';
    _headerDiscountLabel = '';
    _headerDiscountType = '';
    _headerDiscountValue = 0;
    notifyListeners();
  }

  void clear() {
    _lines.clear();
    clearHeaderDiscount();
  }
}
