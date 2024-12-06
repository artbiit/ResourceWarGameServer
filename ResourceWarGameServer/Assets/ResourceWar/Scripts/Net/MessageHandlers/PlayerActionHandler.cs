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
            // 정확한 액션타입이 나오지 않아서 일단 9999이면 실패인 것으로 처리함
            // 타겟 오브젝트도 빈 손인 경우가 없어서 일단 1000으로 함
            uint PlayerActionType = 9999;
            uint PlayerTargetObjectId = 1000; // 나중에 패킷검증할 때 써야할 듯
            bool isSuccess = true;
            if (packet.Payload is C2SPlayerActionReq playerAction)
            {
                PlayerActionType = playerAction.ActionType;
                PlayerTargetObjectId = playerAction.TargetObjectId;
            }
            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("TeamChangeHandler: Token is null or empty.");
                isSuccess = false;
            }
            // 행동이 성공인지 실패인지를 가려야 하는데 이건 좀 생각해봐야 할 듯
            // 해당 오브젝트가플레이어가 상호작용할 수 있는 거리 내에 있나? 이외에 어떻게 가려야 하지?
            // 그럼 저게 true로 반환되면 플레이어가 들고 있는 아이템이 바뀌어야한다.
            // 먄약 용광로나 제제소같은데에 넣으면 아이템이 없어져야 하고
            var actionPacket = new Packet
            {
                PacketType = PacketType.PLAYER_ACTION_RESPONSE,
                Payload = new Protocol.S2CPlayerActionRes
                {
                    ActionType = PlayerActionType,
                    TargetObjectId = PlayerTargetObjectId,
                    Success = isSuccess,
                }
            };
            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.PlayerSync, packet);
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForUser, actionPacket);
            return null;
        }
    }
}
