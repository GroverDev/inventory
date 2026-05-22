import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import type { Form } from '@/modules/user-account/models/form.model';

const { post, get, put, del } = useApi();

const useForm = () => {

  const getForms = async (nameForm: string): Promise<ResponseArray<Form>> => {
    return await get<ResponseArray<Form>>(`Forms?nameForm=${nameForm}`);
  }

  const getFormById = async (id: number): Promise<ResponseObject<Form>> => {
    return await get<ResponseObject<Form>>(`Forms/${id}`);
  }

  const createForm = async (form: Form): Promise<ResponseObject<number>> => {
    return await post<ResponseObject<number>>('Forms', {
        nameForm: form.NameForm,
        description: form.Description,
        orden: form.Orden,
        route: form.Route,
        controller: form.Controller,
        iconCss: form.IconCss,
        showMenu: form.ShowMenu,
        isFormRegister: form.IsFormRegister,
        moduleId: form.ModuleId,
        formId: form.FormId // Often ignored on create but included in Postman body
    });
  }

  const updateForm = async (form: Form): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Forms/${form.Id}`, {
        id: form.Id,
        formId: form.FormId,
        nameForm: form.NameForm,
        description: form.Description,
        orden: form.Orden,
        route: form.Route,
        controller: form.Controller,
        iconCss: form.IconCss,
        showMenu: form.ShowMenu,
        isFormRegister: form.IsFormRegister,
        moduleId: form.ModuleId
    });
  }

  const deleteForm = async (id: number): Promise<ResponseObject<boolean>> => {
      return await del<ResponseObject<boolean>>(`Forms/${id}`);
  }

  return { getForms, getFormById, createForm, updateForm, deleteForm }
}
export default useForm;
