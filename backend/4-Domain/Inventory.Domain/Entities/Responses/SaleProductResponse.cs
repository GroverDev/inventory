namespace Inventory.Domain;

public class SaleProductResponse
{
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string SellerName { get; set; } = "";
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal HeaderDiscountAmount { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }

        /// <summary>Suma de las devoluciones de la venta (v_sales_net.total_returned).</summary>
        public decimal TotalReturned { get; set; }

        /// <summary>Total menos lo devuelto: lo que quedó efectivamente cobrado.</summary>
        public decimal NetTotal { get; set; }

        /// <summary>activa | con_devolucion | anulada, derivado en v_sales_net.</summary>
        public string SaleStatus { get; set; } = "";
        public List<SaleProductDetailResponse> Detail { get; set; } = [];
        public List<SalePaymentResponse> Payments { get; set; } = [];
        public List<SaleReturnResponse> Returns { get; set; } = [];
}


