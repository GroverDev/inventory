import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import type { ConnectedUser, Session } from '@/modules/user-account/models/session.model';

const { get, del } = useApi();

const useSessions = () => {

  const getConnectedUsers = async (): Promise<ResponseArray<ConnectedUser>> => {
    return await get<ResponseArray<ConnectedUser>>('Sessions/connected');
  }

  const getUserSessions = async (uuid: string): Promise<ResponseArray<Session>> => {
    return await get<ResponseArray<Session>>(`Sessions/user/${uuid}`);
  }

  const closeSession = async (id: number): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Sessions/${id}`);
  }

  const closeAllUserSessions = async (uuid: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Sessions/user/${uuid}`);
  }

  return { getConnectedUsers, getUserSessions, closeSession, closeAllUserSessions }
}
export default useSessions;
