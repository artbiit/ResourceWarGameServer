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
        private async UniTask<Packet> SurrenderHandler(ReceivedPacket packet)
        {
            var result = new Packet
            {
                PacketType = PacketType.SURRENDER_RESPONSE
            };

            var resultCode = SurrenderResultCode.SUCCESS;

            // 패킷 검증
            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("GameStartHandler: Token is null or empty.");
                resultCode = SurrenderResultCode.FAIL;
            }
            
            if (resultCode == SurrenderResultCode.SUCCESS)
            {
                await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.SurrenderNoti, packet);
            }

            result.Token = "";
            result.Payload = new S2CSurrenderRes
            {
                SurrenderResultCode = (uint)resultCode
            };

            return result;
        }
    }
}
