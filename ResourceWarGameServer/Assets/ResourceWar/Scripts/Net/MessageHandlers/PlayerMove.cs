using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using Logger = ResourceWar.Server.Lib.Logger;


namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> PlayerMove(ReceivedPacket packet)
        {
            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.PlayerSync, packet);
            return null;
        }

        

        
    }
}
