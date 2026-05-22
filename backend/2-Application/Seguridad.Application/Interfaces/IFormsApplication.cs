using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IFormsApplication
{
    public Task<Response<List<Forms>>> GetFormsXRolId(int rolId);
    public Task<Response<int>> CreateForm(FormsRequest formRequest, int createdBy);
    public Task<Response<bool>> UpdateForm(FormsRequest formRequest, int modifiedBy);
    public Task<Response<bool>> DeleteForm(int id, int idUserModified);
    public Task<Response<List<FormsResponse>>> GetForms(string nameForm);
    public Task<Response<FormsResponse>> GetForm(int id);
}
