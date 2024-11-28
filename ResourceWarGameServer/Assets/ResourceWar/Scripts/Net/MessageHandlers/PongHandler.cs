using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> PongHandler(ReceivedPacket packet)
        {
            //Logger.Log("받은 패킷 : " + packet.Payload);
            await EventDispatcher<PacketType, Packet>.Instance.NotifyAsync(PacketType.PONG_RESPONSE, packet);
            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.UpdatePlayerReceiveTime, packet);
            return null;
        }
    }
}
