using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> GameStartHandler(ReceivedPacket packet)
        {
            // 패킷 검증
            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("GameStartHandler: Token is null or empty.");
            }

            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.GameStart, packet);

            return null;
        }
    }
}