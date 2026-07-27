
import axios, { type AxiosInstance } from 'axios';
// Obtenenmos instancia de axios con la configuración base
export const getApi = (): AxiosInstance => {
    return axios.create({
        baseURL: import.meta.env.VITE_API_SERVICIOS,
        // El refresh token viaja en una cookie HttpOnly que JavaScript no
        // puede leer; sin esto el navegador no la enviaría al ser otro origen.
        withCredentials: true,
    })
};

