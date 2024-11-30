using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using UnityEditor.PackageManager;
using Logger = ResourceWar.Server.Lib.Logger;


namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> PlayerActionHandler(ReceivedPacket packet)
        {
            Logger.Log($"패킷 토큰은 : {packet.Token},클라아이디는 : {packet.ClientId}");
            // "ActionType", "TargetObjectId", "Success"
            // 정확한 액션타입이 나오지 않아서 일단 9999이면 실패인 것으로 처리함
            uint PlayerActionType = 9999;
            uint PlayerTargetObjectId = 1000;
            if (packet.Payload is C2SPlayerActionReq playerAction)
            {
                PlayerActionType = playerAction.ActionType;
                PlayerTargetObjectId = playerAction.TargetObjectId;
            }
            var actionpacket = new Packet
            {
                PacketType = PacketType.PLAYER_ACTION_RESPONSE,
                Payload = new Protocol.S2CPlayerActionRes
                {
                    ActionType = PlayerActionType,
                    TargetObjectId = PlayerTargetObjectId,
                    Success = true,
                }

            };
            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.PlayerSync, packet);
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForUser, actionpacket);
            return null;
        }
    }
}
