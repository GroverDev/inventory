
import axios, { type AxiosInstance } from 'axios';
// Obtenenmos instancia de axios con la configuración base
export const getApi = (): AxiosInstance => {
    return axios.create({
        baseURL: import.meta.env.VITE_API_SERVICIOS,
    })
};

