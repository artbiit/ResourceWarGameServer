using Cysharp.Threading.Tasks;
using GluonGui.Dialog;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> TeamChangeHandler(ReceivedPacket packet)
        {
            var result = new Packet
            {
                PacketType = PacketType.TEAM_CHANGE_RESPONSE
            };

            var resultCode = TeamChangeResultCode.SUCCESS;
            C2STeamChangeReq teamChangeMessage = (C2STeamChangeReq)packet.Payload;


            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("TeamChangeHandler: Token is null or empty.");
                resultCode = TeamChangeResultCode.FAIL;
            }

            if (resultCode == TeamChangeResultCode.SUCCESS)
            {
                // 팀 변경 처리
                await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.TeamChange, packet);
            }

            result.Token = "";
            result.Payload = new S2CTeamChangeRes
            {
                TeamChangeResultCode = (uint)resultCode
            };

            return result;
        }
    }
    
}
