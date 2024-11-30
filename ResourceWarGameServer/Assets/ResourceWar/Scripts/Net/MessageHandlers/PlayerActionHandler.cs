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
            var actionpacket = new Packet
            {
                PacketType = PacketType.PLAYER_ACTION_RESPONSE,
                Payload = new Protocol.S2CPlayerActionRes
                {
                    ActionType = 1,
                    TargetObjectId = 1,
                    Success = true,
                }

            };
            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.PlayerSync, packet);
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForUser, actionpacket);
            return null;
        }
    }
}
