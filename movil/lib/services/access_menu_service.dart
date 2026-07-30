import '../core/network/api_client.dart';
import '../models/access_menu.dart';

class AccessMenuService {
  AccessMenuService(this._api);
  final ApiClient _api;

  /// GET api/AccessMenu — árbol de formularios habilitados para el usuario,
  /// con las banderas de permisos (unión de todos sus roles).
  Future<List<AccessMenu>> getMenu() async {
    final res = await _api.get<List<AccessMenu>>(
      'api/AccessMenu',
      (data) => (data as List? ?? [])
          .map((e) => AccessMenu.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
    return res.data ?? const [];
  }
}
