using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> PongHandler(Packet packet)
        {
           await EventDispatcher<PacketType, Packet>.Instance.NotifyAsync(PacketType.PONG_RESPONSE, packet);
           return null;
        }
    }
}
