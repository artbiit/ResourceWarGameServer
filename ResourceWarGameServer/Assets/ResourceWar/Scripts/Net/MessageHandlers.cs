using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;
using Cysharp.Threading.Tasks;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private Dictionary<PacketType, Func<ReceivedPacket, UniTask<Packet>>> Handlers = new();

        public MessageHandlers() : base() {
            Handlers.Add(PacketType.PONG_RESPONSE, this.PongHandler);
            Handlers.Add(PacketType.SIGN_IN_REQUEST, this.SignInHandler);
            Handlers.Add(PacketType.PLAYER_MOVE, this.PlayerMove);
            Handlers.Add(PacketType.AUTHORIZE_REQUEST, this.AuthorizeRequestHandler);
            Handlers.Add(PacketType.JOIN_ROOM_REQUEST, this.PlayerJoinRoom);
        }


        public async UniTask<Packet> ExecuteHandler(ReceivedPacket packet)
        {
            Packet result = null;

            if (Handlers.TryGetValue(packet.PacketType, out var handler))
            {
                result = await handler(packet);
                //null이면 반환할 데이터 없음을 의미
                if (result != null && !PacketUtils.IsSameMessageType(result.Payload, result.PacketType))
                {
                    throw new InvalidOperationException($"Mismatch between returned message type and packetType. : {packet}");
                }
            }
     
            return result;
        }
    }
}
