using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocol;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        public async UniTask<Packet> QuitRoomHandler(ReceivedPacket packet)
        {
            var resultCode = AuthorizeResultCode.SUCCESS;
            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.Log($"Token is null : {packet.Token}");
                resultCode = AuthorizeResultCode.FAIL;
            }

            if (resultCode == AuthorizeResultCode.SUCCESS)
            {
                await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.
                    Instance.NotifyAsync(GameManager.GameManagerEvent.QuitLobby, packet);
            }

            return null;
        }
    }
}
