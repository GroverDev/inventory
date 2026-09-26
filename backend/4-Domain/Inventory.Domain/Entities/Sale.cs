using Common.Utilities;

namespace Inventory.Domain;

public class Sale: Audit
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }
        public Guid? CashSessionId { get; set; }
        public Guid? HeaderDiscountId { get; set; }
        public decimal HeaderDiscountAmount { get; set; }
        /// <summary>
        /// No se graba. La venta la pide el servicio: si es de un cajero sin
        /// autorización, un producto sin seguimiento no se vende por encima de su
        /// saldo (ver fn_asignar_fefo, app.exigir_stock).
        /// </summary>
        public bool EnforceStock { get; set; }
        public List<SaleDetail> Detail { get; set; } = [];
        public List<SalePayment> Payments { get; set; } = [];
    }