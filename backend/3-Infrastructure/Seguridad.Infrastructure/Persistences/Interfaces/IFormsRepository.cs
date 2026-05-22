using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IFormsRepository
{
    public Task<List<Forms>> GetFormsXRolId(int rolId);
    public Task<int> CreateForm(Forms form);
    public Task<int> UpdateForm(Forms form);
    public Task<int> DeleteForm(int id, int idUserModified);
    public Task<List<Forms>> GetForms(string nameForm);
    public Task<Forms> GetForm(int id);
}
