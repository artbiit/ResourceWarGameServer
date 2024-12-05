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
        private async UniTask<Packet> PlayerMoveAreaHandler(ReceivedPacket packet)
        {
            Logger.Log($"패킷 토큰은 : {packet.Token},클라아이디는 : {packet.ClientId}");
            uint joinMapResultCode = 0;
            uint DestinationArea = 99999;
            if (packet.Payload is C2SMoveToAreaMapReq playerAction)
            {
                if(playerAction.CurrentAreaType == playerAction.DestinationAreaType)
                {
                    joinMapResultCode = 1;
                    DestinationArea = playerAction.DestinationAreaType;
                    Logger.LogError("맵 이동이 아님!");
                }
            }
            var Packet = new Packet
            {
                PacketType = PacketType.PLAYER_ACTION_RESPONSE,
                Token = packet.Token,
                Payload = new Protocol.S2CMoveToAreaMap
                {
                    JoinMapResultCode = joinMapResultCode,
                    DestinationAreaType = DestinationArea // 도착 맵의 번호를 알려줘야 할 듯
                }

            };
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.PlayerSync, Packet);
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForUser, Packet);
            return null;
        }
    }
}
