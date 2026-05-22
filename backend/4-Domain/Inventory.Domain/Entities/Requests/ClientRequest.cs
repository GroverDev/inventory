namespace Inventory.Domain.Entities.Requests;

public class CustomerRequest
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cellphone { get; set; } = "";
    public bool IsActive { get; set; }
}
