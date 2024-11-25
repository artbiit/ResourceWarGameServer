using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{

    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> PlayerMove(Packet packet)
        {
            Logger.Log(packet.Payload.ToString());
            
            return null;
        }
    }
}