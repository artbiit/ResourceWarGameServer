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
        private async UniTask<Packet> PlayerIsReadyChangeHandler(ReceivedPacket packet)
        {
            var result = new Packet
            {
                PacketType = PacketType.PLAYER_IS_READY_CHANGE_RESPONSE
            };

            var resultCode = PlayerIsReadyChangeResultCode.SUCCESS;
            C2SPlayerIsReadyChangeReq playerIsReadyChangeMessage = null;

            // 패킷 검증
            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("TeamChangeHandler: Token is null or empty.");
                resultCode = PlayerIsReadyChangeResultCode.FAIL;
            }
            
            // Payload 검증
            if (packet.Payload is C2SPlayerIsReadyChangeReq payload)
            {
                playerIsReadyChangeMessage = payload;
            }

            if (resultCode == PlayerIsReadyChangeResultCode.SUCCESS)
            {
                await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.PlayerIsReadyChanger, packet);
            }

            result.Token = "";
            result.Payload = new S2CPlayerIsReadyChangeRes
            {
                PlayerIsReadyChangeResultCode = (uint)resultCode
            };

            return result;
        }
    }
}
