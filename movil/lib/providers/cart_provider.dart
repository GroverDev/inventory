import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/foundation.dart';

import '../models/catalog.dart';
import '../models/product.dart';
import '../models/sale.dart';

double _round(double v) => double.parse(v.toStringAsFixed(2));

/// Carrito del punto de venta (cliente + líneas + descuentos por línea y
/// global). Pertenece al turno de caja, no a la app: `clear()` lo vacía
/// entero, incluido el cliente, al cerrar sesión y al cerrar caja.
class CartProvider extends ChangeNotifier {
  final List<SaleLine> _lines = [];
  Customer? _customer;

  // ── Descuento global (cabecera) ──────────────────────────
  String _headerDiscountId = '';
  String _headerDiscountLabel = '';
  String _headerDiscountType = '';
  double _headerDiscountValue = 0;

  List<SaleLine> get lines => List.unmodifiable(_lines);
  bool get isEmpty => _lines.isEmpty;
  int get itemCount => _lines.fold(0, (s, l) => s + l.quantity);
  Customer? get customer => _customer;

  void setCustomer(Customer? c) {
    _customer = c;
    notifyListeners();
  }

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

  /// Un producto serializado no entra por acá: hay que decir qué unidad sale
  /// (ver `setSerializedLine`), así que quien llama a `add` para uno de esos
  /// productos se equivocó de método — se ignora en vez de sumar una unidad
  /// sin número de serie.
  void add(Product product) {
    if (product.usesSerial) return;
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

  /// Agrega o reemplaza la línea de un producto serializado. La cantidad la
  /// determina cuántas unidades se eligieron: no hay forma de vender 3
  /// unidades indicando 2 números. Una lista vacía la quita del carrito.
  /// Espejo de `confirmSerials` en `PointOfSaleView.vue`.
  void setSerializedLine(Product product, List<String> serials) {
    final idx = _lines.indexWhere((l) => l.product.id == product.id);
    if (serials.isEmpty) {
      if (idx >= 0) removeWithUndo(idx);
      return;
    }
    if (idx >= 0) {
      _lines[idx].serialNumbers = List.of(serials);
      _lines[idx].quantity = serials.length;
    } else {
      _lines.add(SaleLine(
          product: product, quantity: serials.length, serialNumbers: List.of(serials)));
    }
    notifyListeners();
  }

  void increment(SaleLine line) {
    // Una línea serializada no se ajusta con +/-: hay que decir qué unidad
    // entra o sale, y eso lo resuelve el selector, no este método.
    if (line.serialNumbers.isNotEmpty) return;
    line.quantity++;
    notifyListeners();
  }

  void decrement(SaleLine line) {
    if (line.serialNumbers.isNotEmpty) return;
    if (line.quantity <= 1) {
      final idx = _lines.indexOf(line);
      if (idx >= 0) removeWithUndo(idx);
      return;
    }
    line.quantity--;
    notifyListeners();
  }

  void decrementByProduct(String productId) {
    final line = _find(productId);
    if (line != null) decrement(line);
  }

  void remove(SaleLine line) {
    final idx = _lines.indexOf(line);
    if (idx >= 0) removeWithUndo(idx);
  }

  // ── Deshacer el último producto quitado ───────────────────
  // En una tablet es fácil tocar el botón equivocado: quitar no pide
  // confirmación (frenaría cada venta), pero deja unos segundos para
  // deshacerlo. Espejo de `removeWithUndo`/`undoRemove` en
  // `PointOfSaleView.vue` (mismos 6 segundos de ventana).
  static const _undoWindow = Duration(seconds: 6);
  SaleLine? _lastRemoved;
  int _lastRemovedIndex = 0;
  Timer? _undoTimer;

  /// La última línea quitada, mientras siga vigente la ventana para
  /// deshacer. La usa la UI solo para decidir si vale la pena ofrecer el
  /// botón; el que manda es [undoRemove].
  SaleLine? get lastRemoved => _lastRemoved;

  void removeWithUndo(int index) {
    final line = _lines.removeAt(index);
    _undoTimer?.cancel();
    _lastRemoved = line;
    _lastRemovedIndex = index;
    _undoTimer = Timer(_undoWindow, () {
      _lastRemoved = null;
      notifyListeners();
    });
    notifyListeners();
  }

  void undoRemove() {
    _undoTimer?.cancel();
    final removed = _lastRemoved;
    _lastRemoved = null;
    if (removed == null) return;
    // Si mientras tanto volvió a agregar el mismo producto, restaurar
    // duplicaría la línea: se deja como está.
    if (_lines.any((l) => l.product.id == removed.product.id)) {
      notifyListeners();
      return;
    }
    _lines.insert(_lastRemovedIndex.clamp(0, _lines.length), removed);
    notifyListeners();
  }

  void _clearUndo() {
    _undoTimer?.cancel();
    _lastRemoved = null;
  }

  @override
  void dispose() {
    _undoTimer?.cancel();
    super.dispose();
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
    _clearUndo();
    _lines.clear();
    _customer = null;
    clearHeaderDiscount();
  }

  /// Reemplaza carrito completo (líneas, cliente y descuento global) de una
  /// sola vez, para retomar una venta en espera. Un solo `notifyListeners` en
  /// vez de uno por campo — a diferencia de `clear()` + agregar línea por
  /// línea, que reconstruiría la UI en cada paso intermedio.
  void replaceAll({
    required List<SaleLine> lines,
    Customer? customer,
    String headerDiscountId = '',
    String headerDiscountLabel = '',
    String headerDiscountType = '',
    double headerDiscountValue = 0,
  }) {
    _clearUndo();
    _lines
      ..clear()
      ..addAll(lines);
    _customer = customer;
    _headerDiscountId = headerDiscountId;
    _headerDiscountLabel = headerDiscountLabel;
    _headerDiscountType = headerDiscountType;
    _headerDiscountValue = headerDiscountValue;
    notifyListeners();
  }
}
