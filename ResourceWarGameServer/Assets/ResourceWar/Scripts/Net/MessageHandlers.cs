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
        private Dictionary<PacketType, Func<Packet, UniTask<Packet>>> Handlers = new Dictionary<PacketType, Func<Packet, UniTask<Packet>>>();

        public MessageHandlers() : base() {
            Handlers.Add(PacketType.PONG_RESPONSE, this.PongHandler);
            Handlers.Add(PacketType.SIGN_IN_REQUEST, this.SignInHandler);

            //용광로
            Handlers.Add(PacketType.FURNACE_RESPONSE, this.HandleAddItemToFurnace);
        }


        public async UniTask<Packet> ExecuteHandler(Packet packet)
        {
            Packet result = null;

            if (Handlers.TryGetValue(packet.PacketType, out var handler))
            {
                Logger.Log(handler.ToString());
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
