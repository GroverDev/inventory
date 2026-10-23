/**
 * Nodo del menú de accesos que devuelve el backend (GET AccessMenu).
 * Árbol de 3 niveles: sección (título) -> grupo -> formulario.
 * `url` es el nombre de la ruta del formulario y también su clave de permisos.
 */
export interface AccessMenu {
  IdFormulario: number
  IdFormularioPadre: number
  titulo: string
  url: string
  SeMuestraEnMenu: boolean
  EsFormulario: boolean
  CanCreate: boolean
  CanRead: boolean
  CanUpdate: boolean
  CanDelete: boolean
  Children: AccessMenu[]
}
