using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

public struct PlayerStates
{
    public uint PlayerId;
    public byte ActionType;
    public Position Position;
    public uint EquippedItem;
}

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

            PlayerStates playerState = new()
            {
                PlayerId = (uint)packet.ClientId,
                ActionType = 1,
                Position = position,
                EquippedItem = 1,
            };
            Logger.Log($"패킷 토큰은 : {packet.Token},클라아이디는 : {packet.ClientId}");

           await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.AddNewPlayer, packet);
            NotifyClient("플레이어 이동중인가?", playerState, packet.Token);
            return null;
        }

        

        private void NotifyClient(string message, PlayerStates playerState, string token)
        {
            var protoPlayerState = new Protocol.PlayerState
            {
                PlayerId = playerState.PlayerId,
                ActionType = playerState.ActionType,
                Position = new Protocol.Position
                {
                    X = playerState.Position.X,
                    Y = playerState.Position.Y,
                    Z = playerState.Position.Z
                },
                EquippedItem = playerState.EquippedItem
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
            EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance
                .NotifyAsync(GameManager.GameManagerEvent.SendPacketForAll, packet)
                .Forget();
        }
    }
}
