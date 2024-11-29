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
        private async UniTask<Packet> PlayerJoinRoom(ReceivedPacket packet)
        {
            Logger.Log($"패킷 토큰은 : {packet.Token},클라아이디는 : {packet.ClientId}");
            // 이건 추후에 플레이어 등록으로 옮겨야함
            await EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.NotifyAsync(GameManager.GameManagerEvent.AddNewPlayer, packet);

            var sendTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var pingpacket = new Packet
            {
                PacketType = PacketType.PING_REQUEST,

                //Token = "",
                Payload = new Protocol.S2CPingReq
                {
                    ServerTime = sendTime
                }
            };
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForUser, pingpacket);
            return null;
        }
    }
}
