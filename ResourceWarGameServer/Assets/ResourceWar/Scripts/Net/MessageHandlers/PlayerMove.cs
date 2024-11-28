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
        private async UniTask<Packet> PlayerMove(ReceivedPacket packet)
        {
            Logger.Log(packet.Payload.ToString());
            Logger.Log(packet);
            Position position = new Position();

            if (packet.Payload is C2SPlayerMove playerMove)
            {
                position = playerMove.Position;
                float x = position.X;
                float y = position.Y;
                float z = position.Z;

                Logger.Log($"Player Position: X = {x}, Y = {y}, Z = {z}");
            }
            Logger.Log($"패킷 토큰은 : {packet.Token},클라아이디는 : {packet.ClientId}");

            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.AddNewPlayer, packet);
            PlayerSyncNotify("플레이어 이동중인가?", (uint)packet.ClientId, 1, position, 1, packet.Token);
            var pingpacket = new Packet
            {
                PacketType = PacketType.PING_REQUEST,

                //Token = "",
                Payload = new Protocol.S2CPingReq
                {
                    ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            };

            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForUser, pingpacket);
            
            return null;
        }

        

        private async void PlayerSyncNotify(string message, uint ClientId, byte ActionType, Position position, uint EquippedItem, string token)
        {
            var protoPlayerState = new Protocol.PlayerState
            {
                PlayerId = ClientId,
                ActionType = ActionType,
                Position = new Protocol.Position
                {
                    X = position.X,
                    Y = position.Y,
                    Z = position.Z
                },
                EquippedItem = EquippedItem
            };

            var packet = new Packet
            {
                PacketType = PacketType.SYNC_PLAYERS_NOTIFICATION,
                
                //Token = "", // 특정 클라이언트에게 전송 시 설정
                Payload = new Protocol.S2CSyncPlayersNoti {
                    PlayerStates = { protoPlayerState },
                }
            };
            Logger.Log(packet);
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance
                .NotifyAsync(GameManager.GameManagerEvent.SendPacketForAll, packet);
        }
    }
}
