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
            //로비 관련
            Handlers.Add(PacketType.QUIT_ROOM_REQUEST, this.QuitRoomHandler);
            Handlers.Add(PacketType.JOIN_ROOM_REQUEST, this.PlayerJoinRoom);
            Handlers.Add(PacketType.TEAM_CHANGE_REQUEST, this.TeamChangeHandler);
            Handlers.Add(PacketType.GAME_START_REQUEST, this.GameStartHandler);
            Handlers.Add(PacketType.PLAYER_IS_READY_CHANGE_REQUEST, this.PlayerIsReadyChangeHandler);
            Handlers.Add(PacketType.LOAD_PROGRESS_NOTIFICATION, this.LoadProgressHandler);
            Handlers.Add(PacketType.SURRENDER_REQUEST, this.SurrenderHandler);
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
                    throw new InvalidOperationException($"Mismatch between returned message type and packetType. : {packet}\n{result.Payload.GetType().Name}|{result.PacketType}");
                }
            }
     
            return result;
        }
    }
}
