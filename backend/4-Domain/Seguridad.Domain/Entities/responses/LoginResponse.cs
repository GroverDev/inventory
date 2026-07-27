namespace Seguridad.Domain;

public class LoginResponse
{
        public int UserId { get; set; }
        public string Uuid { get; set; } = Guid.Empty.ToString();
        public int SesionId { get; set; }
        public string FullName { get; set; }="";
        public string UserName { get; set; }="";
        public string Email { get; set; }="";
        public bool ChangePassword { get; set; }
        public string Token { get; set; }="";

        /// <summary>
        /// Solo se entrega a clientes que lo soportan (móvil). Vacío en web,
        /// donde se omite de la respuesta por ser null/vacío.
        /// </summary>
        public string RefreshToken { get; set; }="";

        public bool RequireTotp { get; set; }
        public bool TotpSetupRequired { get; set; }
        public string TotpSessionToken { get; set; }="";
        public int RolId { get; set; }
        public string RolName { get; set; } = "";
}
