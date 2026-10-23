import axios, { type AxiosInstance } from 'axios'

// Instancia de axios con la configuración base del backend.
export const getApi = (): AxiosInstance =>
  axios.create({
    baseURL: import.meta.env.VITE_API_SERVICIOS,
    // El refresh token viaja en una cookie HttpOnly que JavaScript no
    // puede leer; sin esto el navegador no la enviaría al ser otro origen.
    withCredentials: true,
  })
