using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> AuthorizeRequestHandler(ReceivedPacket packet)
        {
            var result = new Packet();
            result.PacketType = PacketType.AUTHORIZE_RESPONSE;
            var resultCode = AuthorizeResultCode.SUCCESS;
          
            if (TcpServer.Instance.TryGetClient(packet.ClientId, out var clientHandler)){
                if (clientHandler.IsAuthorized)
                {
                    resultCode = AuthorizeResultCode.ALREADY_AUTHORIZED;
                }
                else
                {
                    if(string.IsNullOrWhiteSpace(packet.Token))
                    {
                        Logger.Log($"Token is null : {packet.Token}");
                        resultCode = AuthorizeResultCode.FAIL;
                    }
                    else
                    {
                        var userSessionByRedis = await UserRedis.GetUserSession(packet.Token);
                        if (userSessionByRedis == null)
                        {
                            Logger.Log($"userSessionByRedis is null : {packet.Token}");
                            resultCode = AuthorizeResultCode.FAIL;
                        }
                        else
                        {
                           var userSession = new UserSession(userSessionByRedis);
                           if(userSession.ExpirationTime < UnixTime.Now())
                            {
                                Logger.Log($"userSession is expired");
                                resultCode = AuthorizeResultCode.FAIL;
                            }
                        }
                    }
                }
            }

#if UNITY_EDITOR
            if(packet.Token.StartsWith("master"))
            {
                resultCode = AuthorizeResultCode.SUCCESS;
            }
#endif
            var payload = new S2CAuthorizeRes { AuthorizeResultCode = (uint)resultCode };;
            result.Payload = payload;
            result.Token = "";
            if (resultCode == AuthorizeResultCode.SUCCESS)
            {
                clientHandler.Authorized();
                await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.AddNewPlayer, packet);
            }
            result.Payload = payload;
            return result;
        }
    }
}
