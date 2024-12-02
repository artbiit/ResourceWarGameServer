using Cysharp.Threading.Tasks;
using GluonGui.Dialog;
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
        private async UniTask<Packet> TeamChangeHandler(ReceivedPacket packet)
        {
            var result = new Packet
            {
                PacketType = PacketType.TEAM_CHANGE_RESPONSE
            };

            var resultCode = TeamChangeResultCode.SUCCESS;
            C2STeamChangeReq teamChangeMessage = (C2STeamChangeReq)packet.Payload;

            // 패킷 검증
            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("TeamChangeHandler: Token is null or empty.");
                resultCode = TeamChangeResultCode.FAIL;
            }

            // teamIndex 기본값 설정
            var teamIndex = 0; // Default to 0
            if (resultCode == TeamChangeResultCode.SUCCESS)
            {
                teamIndex = (teamChangeMessage.TeamIndex == 0) ? 0 : (int)teamChangeMessage.TeamIndex;
                teamChangeMessage.TeamIndex = (uint)teamIndex;
                Logger.Log($"TeamChangeHandler: Received teamIndex is {teamIndex}. Defaulting to 0 if not set.");
            }

            // 새로운 ReceivedPacket 생성
            var updatedPacket = new ReceivedPacket(packet.ClientId)
            {
                PacketType = packet.PacketType,
                Token = packet.Token,
                Payload = teamChangeMessage
            };

            if (resultCode == TeamChangeResultCode.SUCCESS)
            {
                // 팀 변경 처리
                await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.TeamChange, updatedPacket);
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
