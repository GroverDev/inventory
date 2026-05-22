using Mapster;
using Seguridad.Domain;
using Seguridad.Domain.Entities;
using Seguridad.Domain.Entities.requests;

namespace Seguridad.Application.Mappers;

public class SecurityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Users, UserRequest>().TwoWays();
        config.NewConfig<Forms, FormsResponse>().TwoWays();
        config.NewConfig<Modules, ModulesResponse>().TwoWays();
    }
}
