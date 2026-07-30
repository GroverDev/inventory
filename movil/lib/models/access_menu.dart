/// Acciones de un permiso granular por formulario.
enum PermAction { create, read, update, delete }

/// Ruta del formulario de productos en seguridad (`sec.forms.route`); es la
/// clave con la que se consultan sus permisos.
const kProductsForm = 'products-admin';

/// Espejo de `AccessMenu` del backend (`GET api/AccessMenu`).
///
/// La clave del permiso es [url], que corresponde a `sec.forms.route`
/// (ej. `products-admin`), igual que en la web.
class AccessMenu {
  AccessMenu({
    required this.url,
    required this.title,
    required this.canCreate,
    required this.canRead,
    required this.canUpdate,
    required this.canDelete,
    required this.children,
  });

  final String url;
  final String title;
  final bool canCreate;
  final bool canRead;
  final bool canUpdate;
  final bool canDelete;
  final List<AccessMenu> children;

  factory AccessMenu.fromJson(Map<String, dynamic> j) => AccessMenu(
        url: j['url'] ?? '',
        title: j['titulo'] ?? '',
        canCreate: j['CanCreate'] ?? false,
        canRead: j['CanRead'] ?? false,
        canUpdate: j['CanUpdate'] ?? false,
        canDelete: j['CanDelete'] ?? false,
        children: (j['Children'] as List?)
                ?.map((e) => AccessMenu.fromJson(e as Map<String, dynamic>))
                .toList() ??
            const [],
      );

  /// Se persiste con las mismas claves del backend para poder releerlo con
  /// [AccessMenu.fromJson] sin conversiones extra.
  Map<String, dynamic> toJson() => {
        'url': url,
        'titulo': title,
        'CanCreate': canCreate,
        'CanRead': canRead,
        'CanUpdate': canUpdate,
        'CanDelete': canDelete,
        'Children': children.map((e) => e.toJson()).toList(),
      };

  bool allows(PermAction action) => switch (action) {
        PermAction.create => canCreate,
        PermAction.read => canRead,
        PermAction.update => canUpdate,
        PermAction.delete => canDelete,
      };
}
