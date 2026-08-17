using Common.Utilities;

namespace Inventory.Domain;

public class Product : Audit
{
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";

        public string Description { get; set; } = "";
        public decimal SalePrice { get; set; }
        public Guid UomId { get; set; }
        public int CurrentStock { get; set; }
        public bool IsActive { get; set; }
        public int MinReorderQuantity { get; set; }
        public bool AvailableInPos { get; set; }

        /// <summary>
        /// La venta exige un respaldo: receta médica en farmacia, permiso en una
        /// ferretería con químicos controlados. Nombre genérico a propósito — el
        /// comportamiento es del núcleo, no de un rubro.
        /// </summary>
        public bool RequiresAuthorization { get; set; }
        /// <summary>
        /// Laboratorio o proveedor. Opcional: no toda la mercadería que vende
        /// una farmacia tiene laboratorio (accesorios, limpieza, genéricos).
        /// </summary>
        public Guid? LaboratoryId { get; set; }
        /// <summary>
        /// Categoría del producto. Opcional, igual que el laboratorio: la
        /// columna admite NULL y tiene FK, así que un Guid vacío no
        /// referenciaría a nada y la inserción fallaría.
        /// </summary>
        public Guid? CategoryId { get; set; }
        public string BarCode { get; set; } = "";
      
}
