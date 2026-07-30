import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../models/sale.dart';

/// Pantalla de confirmación tras registrar una venta (espejo del modal
/// "Venta Completada" de la web).
class SaleCompletedScreen extends StatelessWidget {
  const SaleCompletedScreen({
    super.key,
    required this.customerName,
    required this.total,
    required this.change,
    required this.payments,
    required this.detail,
    required this.totalLineDiscounts,
    required this.headerDiscountAmount,
  });

  final String customerName;
  final double total;
  final double change;
  final List<SalePayment> payments;
  final List<SaleLine> detail;
  final double totalLineDiscounts;
  final double headerDiscountAmount;

  @override
  Widget build(BuildContext context) {
    final date = DateFormat('dd/MM/yyyy HH:mm').format(DateTime.now());
    return PopScope(
      canPop: false,
      child: Scaffold(
        appBar: AppBar(
          automaticallyImplyLeading: false,
          title: const Text('Venta completada'),
        ),
        body: SafeArea(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(
                child: ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    const Center(
                      child: Icon(Icons.check_circle,
                          color: Colors.green, size: 72),
                    ),
                    const SizedBox(height: 8),
                    Center(
                      child: Text(date,
                          style: const TextStyle(color: Colors.grey)),
                    ),
                    const SizedBox(height: 16),
                    Row(
                      children: [
                        Expanded(
                          child: _statCard('Total cobrado', currency(total),
                              Colors.indigo),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child:
                              _statCard('Vuelto', currency(change), Colors.green),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Row(
                              children: [
                                const Icon(Icons.person_outline, size: 18),
                                const SizedBox(width: 6),
                                Text(customerName,
                                    style: const TextStyle(
                                        fontWeight: FontWeight.w600)),
                              ],
                            ),
                            const Divider(),
                            for (final l in detail)
                              Padding(
                                padding:
                                    const EdgeInsets.symmetric(vertical: 2),
                                child: Row(
                                  children: [
                                    Expanded(
                                      child: Text(
                                          '${l.product.productName} x${l.quantity}'),
                                    ),
                                    Text(currency(l.lineTotal)),
                                  ],
                                ),
                              ),
                            if (totalLineDiscounts > 0)
                              _kv('Desc. por línea',
                                  '− ${currency(totalLineDiscounts)}'),
                            if (headerDiscountAmount > 0)
                              _kv('Desc. global',
                                  '− ${currency(headerDiscountAmount)}'),
                            const Divider(),
                            _kv('TOTAL', currency(total), bold: true),
                            const SizedBox(height: 8),
                            Wrap(
                              spacing: 6,
                              runSpacing: 6,
                              children: [
                                for (final p in payments)
                                  Chip(
                                    label: Text(
                                        '${p.paymentMethodName}: ${currency(p.amountGiven)}'),
                                  ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(16),
                child: FilledButton.icon(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.add),
                  label: const Text('Nueva orden'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _statCard(String label, String value, Color color) => Card(
        color: color,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: Column(
            children: [
              Text(label,
                  style: const TextStyle(color: Colors.white70, fontSize: 12)),
              const SizedBox(height: 4),
              Text(value,
                  style: const TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.bold)),
            ],
          ),
        ),
      );

  Widget _kv(String label, String value, {bool bold = false}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
            Text(value,
                style: TextStyle(
                    fontWeight: bold ? FontWeight.bold : FontWeight.normal)),
          ],
        ),
      );
}
