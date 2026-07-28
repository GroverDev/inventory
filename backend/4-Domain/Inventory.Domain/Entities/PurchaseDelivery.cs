using Common.Utilities;

namespace Inventory.Domain;

public class PurchaseDelivery: Audit
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public DateTime DeliveryDate { get; set; }

    public bool IsActive { get; set; }
    public int PurchaseStatusId { get; set; }

    /// <summary>
    /// Identificador de la operación generado por el cliente. Un reintento con
    /// el mismo uid no vuelve a mover stock (ver índice único en la tabla).
    /// </summary>
    public Guid OperationUid { get; set; }

    public List<PurchaseDeliveryDetail> Detail { get; set; } = [];
}
