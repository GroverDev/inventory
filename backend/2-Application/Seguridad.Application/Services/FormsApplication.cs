using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class FormsApplication(IFormsRepository _formsRepository) : IFormsApplication
{
    public async Task<Response<List<Forms>>> GetFormsXRolId(int rolId)
{
    var resp = new Response<List<Forms>>() { Data = [] };
    try
    {
        resp.Data = await _formsRepository.GetFormsXRolId(rolId);
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }

    return resp;
}

public async Task<Response<int>> CreateForm(FormsRequest formRequest, int createdBy)
{
    Response<int> respuesta = new() { Data = 0 };
    try
    {
        var form = formRequest.Adapt<Forms>();
        form.State = true;
        AuditHelper.SetCreated(form, createdBy);

        respuesta.Data = await _formsRepository.CreateForm(form);
        respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<bool>> UpdateForm(FormsRequest formRequest, int modifiedBy)
{
    Response<bool> respuesta = new();
    try
    {
        var form = formRequest.Adapt<Forms>();
        AuditHelper.SetModified(form, modifiedBy);

        var rowsAffected = await _formsRepository.UpdateForm(form);
        if (rowsAffected <= 0)
            throw new CustomException("No se pudo modificar el formulario");
        respuesta.Data = respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<bool>> DeleteForm(int id, int modifiedBy)
{
    Response<bool> respuesta = new();
    try
    {
        var rowsAffected = await _formsRepository.DeleteForm(id, modifiedBy);
        if (rowsAffected <= 0)
            throw new CustomException("No se pudo eliminar el formulario");
        respuesta.Data = respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<List<FormsResponse>>> GetForms(string nameForm)
{
    Response<List<FormsResponse>> forms = new() { Data = new() };
    try
    {
        var result = await _formsRepository.GetForms(nameForm);
        forms.Data = result.Adapt<List<FormsResponse>>();
        forms.ok = true;
    }
    catch (CustomException ex) { forms.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { forms.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return forms;
}

public async Task<Response<FormsResponse>> GetForm(int id)
{
    Response<FormsResponse> form = new() { Data = new() };
    try
    {
        var result = await _formsRepository.GetForm(id);
        form.Data = result.Adapt<FormsResponse>();
        form.ok = true;
    }
    catch (CustomException ex) { form.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { form.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return form;
}
}
