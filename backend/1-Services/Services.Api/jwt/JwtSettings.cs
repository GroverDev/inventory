using System;

namespace Services.Api.jwt;

public class JwtSettings
{

    public string Secret { get; set; }="";
    public string Issuer { get; set; }="";
    public string Audience { get; set; }="";
    public string TimeToken { get; set; }="";
   
}
