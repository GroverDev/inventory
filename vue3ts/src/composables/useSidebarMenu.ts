/**
 * Comportamiento del menú lateral de Velzon que opera sobre el DOM: marcar el ítem activo según la URL,
 * abrir sus grupos padre y cerrar los hermanos al desplegar uno (los `collapse` de Bootstrap).
 * Lo comparten los layouts vertical, de dos columnas y horizontal.
 */

const byId = (id: string) => document.getElementById(id)

/** Marca activo el icono de la columna izquierda (layout de dos columnas) que apunta a `id`. */
export const activateIconSidebarActive = (id: string): void => {
  const menu = document.querySelector(`#two-column-menu .simplebar-content-wrapper a[href='${id}'].nav-icon`)
  if (menu !== null) menu.classList.add('active')
}

/** Quita el estado activo/abierto de los ítems indicados. */
export const removeActivation = (items: Element[]): void => {
  items.forEach((item) => {
    if (item.classList.contains('menu-link')) {
      if (!item.classList.contains('active')) item.setAttribute('aria-expanded', 'false')
      item.nextElementSibling?.classList.remove('show')
    }
    if (item.classList.contains('nav-link')) {
      item.nextElementSibling?.classList.remove('show')
      item.setAttribute('aria-expanded', 'false')
    }
    item.classList.remove('active')
  })
}

/** Marca el ítem como activo y abre sus grupos padre (y los iconos de la columna izquierda). */
export const activateParentDropdown = (item: Element): void => {
  item.classList.add('active')
  const parentCollapseDiv = item.closest('.collapse.menu-dropdown')
  if (!parentCollapseDiv) return

  parentCollapseDiv.classList.add('show')
  const header = parentCollapseDiv.parentElement!.children[0]
  header.classList.add('active')
  header.setAttribute('aria-expanded', 'true')

  const outer = parentCollapseDiv.parentElement!.closest('.collapse.menu-dropdown')
  if (outer) {
    const outerPrev = outer.previousElementSibling
    if (outerPrev) {
      const grandparent = outerPrev.parentElement!.closest('.collapse.menu-dropdown')
      if (grandparent) {
        activateIconSidebarActive('#' + grandparent.getAttribute('id'))
        grandparent.classList.add('show')
      }
    }
    activateIconSidebarActive('#' + outer.getAttribute('id'))

    const collapse = parentCollapseDiv.parentElement!.closest('.collapse')!
    collapse.classList.add('show')
    collapse.previousElementSibling?.classList.add('active')
    return
  }
  activateIconSidebarActive('#' + parentCollapseDiv.getAttribute('id'))
}

/** Marca el ítem del menú que coincide con la URL actual (o, si no hay, el icono de la columna izquierda). */
export const initActiveMenu = (): void => {
  const pathName = window.location.pathname
  const ul = byId('navbar-nav')
  if (!ul) return

  const items = Array.from(ul.querySelectorAll('a.nav-link'))
  removeActivation(items.filter((x) => x.classList.contains('active')))
  const matching = items.find((x) => x.getAttribute('href') === pathName)
  if (matching) {
    activateParentDropdown(matching)
  } else {
    if (pathName.replace('/', '')) document.body.classList.add('twocolumn-panel')
    activateIconSidebarActive(pathName)
  }
}

const siblingsOf = (elem: Element): Element[] => {
  const siblings: Element[] = []
  let sibling = elem.parentNode!.firstChild
  while (sibling) {
    if (sibling.nodeType === 1 && sibling !== elem) siblings.push(sibling as Element)
    sibling = sibling.nextSibling
  }
  return siblings
}

/** Al abrir un grupo cierra los hermanos; al cerrarlo cierra los anidados. Se llama una vez montado el menú. */
export const bindCollapseHandlers = (): void => {
  document.querySelectorAll('.navbar-nav .collapse').forEach((collapse) => {
    collapse.addEventListener('show.bs.collapse', (e) => {
      e.stopPropagation()
      const closestCollapse = collapse.parentElement!.closest('.collapse')
      if (closestCollapse) {
        closestCollapse.querySelectorAll('.collapse').forEach((sibling) => {
          if (sibling.classList.contains('show')) {
            sibling.classList.remove('show')
            ;(sibling.parentElement!.firstChild as Element).setAttribute('aria-expanded', 'false')
          }
        })
        return
      }
      siblingsOf(collapse.parentElement!).forEach((item) => {
        if (item.childNodes.length > 2) {
          item.firstElementChild!.setAttribute('aria-expanded', 'false')
          item.firstElementChild!.classList.remove('active')
        }
        item.querySelectorAll('*[id]').forEach((withId) => {
          withId.classList.remove('show')
          const first = withId.parentElement!.firstChild as Element
          first.setAttribute('aria-expanded', 'false')
          first.classList.remove('active')
          if (withId.childNodes.length > 2) {
            withId.querySelectorAll('ul li a').forEach((subitem) => {
              if (subitem.hasAttribute('aria-expanded')) subitem.setAttribute('aria-expanded', 'false')
            })
          }
        })
      })
    })

    collapse.addEventListener('hide.bs.collapse', (e) => {
      e.stopPropagation()
      collapse.querySelectorAll('.collapse').forEach((child) => {
        child.classList.remove('show')
        ;(child.parentElement!.firstChild as Element).setAttribute('aria-expanded', 'false')
      })
    })
  })
}

/** Layout horizontal: marca activo el ítem de la ruta indicada una vez pintado el menú. */
export const markActiveByPath = (path: string): void => {
  setTimeout(() => {
    const nav = document.querySelector('#navbar-nav')
    const a = nav?.querySelector(`[href="${path}"]`)
    if (!a) return
    a.classList.add('active')
    const parentCollapseDiv = a.closest('.collapse.menu-dropdown')
    if (!parentCollapseDiv) return
    parentCollapseDiv.classList.add('show')
    const header = parentCollapseDiv.parentElement!.children[0]
    header.classList.add('active')
    header.setAttribute('aria-expanded', 'true')
    if (parentCollapseDiv.parentElement!.closest('.collapse.menu-dropdown')) {
      const collapse = parentCollapseDiv.parentElement!.closest('.collapse')!
      collapse.classList.add('show')
      collapse.previousElementSibling?.classList.add('active')
    }
  }, 1000)
}
