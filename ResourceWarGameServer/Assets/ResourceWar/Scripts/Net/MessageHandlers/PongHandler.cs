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
            
            C2SPongRes pong = (C2SPongRes)packet.Payload;
            await EventDispatcher<(int, int ), long>.Instance.NotifyAsync((packet.ClientId, int.MaxValue + packet.ClientId), pong.ClientTime);
            return null;
        }
    }
}
