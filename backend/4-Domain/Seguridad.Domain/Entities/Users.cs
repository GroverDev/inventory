namespace Seguridad.Domain;

public class Users: Audit
{
        public int Id { get; set; }
        public string UserName { get; set; }="";
        public string Password { get; set; }="";
        public bool ChangePassword { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastAccess { get; set; }
        public string Email { get; set; }="";
        public string FullName { get; set; }="";
        public Guid Uuid { get; set; } = Guid.Empty;

        public int SesionId { get; set; }
}