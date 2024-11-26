using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class GameManager : MonoBehaviour
    {
        public enum State
        {
            CREATING,
            DESTROY,
            LOBBY,
            LOADING,
            PLAYING
        }

        public enum GameManagerEvent
        {
            SendPacketForAll = 0,
            SendPacketForTeam = 1,
            SendPacketForUser = 2,
        }

        public State GameState { get; private set; } = State.CREATING;
        public string GameToken { get; private set; }
        private bool subscribed = false;
        /// <summary>
        /// Token - Player
        /// </summary>
        private Dictionary<string, Player> players = new Dictionary<string, Player>();

        /// <summary>
        /// 용광로를 관리하는 딕셔너리
        /// key: Player의 ClientId, 값: FurnaceClass 객체
        /// </summary>
        private Dictionary<int, FurnaceClass> furnaces = new Dictionary<int, FurnaceClass>();

        public async UniTaskVoid Init()
        {

            GameState = State.CREATING;
            players.Clear();
            furnaces.Clear();
            Subscribes();
        }

        private void Subscribes()
        {
            if (subscribed)
            {
                return;
            }
            subscribed = true;

            var dispatcher = EventDispatcher<GameManagerEvent, Packet>.Instance;
            dispatcher.Subscribe(GameManagerEvent.SendPacketForAll, SendPacketForAll);
            dispatcher.Subscribe(GameManagerEvent.SendPacketForTeam, SendPacketForTeam);
            dispatcher.Subscribe(GameManagerEvent.SendPacketForUser, SendPacketForUser);

        }

        /// <summary>
        /// 용광로를 등록합니다.
        /// 나중에 key값을 GamePlayer의 GameTeam.tema_id로 바꿔주면 끝
        /// </summary>
        public void RegisterFurnace(int clientId, FurnaceClass furnace)
        {
            if (!furnaces.ContainsKey(clientId))
            {
                furnaces[clientId] = furnace;
                Logger.Log($"Furnace registered for ClientId {clientId}");
            }
        }

        /// <summary>
        /// 특정 클라이언틔 용광로를 가져옵니다.
        /// 나중에 key값을 GamePlayer의 GameTeam.tema_id로 바꿔주면 끝
        /// </summary>
        /// <param name="clientId">key값 변경 예정</param>
        /// <returns></returns>
        public FurnaceClass GetFurnaceByClientId(int clientId)
        {
            if (furnaces.TryGetValue(clientId, out var furnace))
            {
                return furnace;
            }

            Logger.LogError($"Furnace not found for ClientId {clientId}");
            return null;
        }


        /// <summary>
        /// 게임 내 모든 유저에게 데이터를 보냅니다.
        /// </summary>
        /// <param name="packet"></param>
        public UniTask SendPacketForAll(Packet packet)
        {
            foreach (var player in players.Values)
            {
                if (TcpServer.Instance.TryGetClient(player.ClientId, out var clientHandler))
                {
                    clientHandler.EnqueueSend(packet);
                }
            }
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 팀에 해당하는 플레이어 한테만 데이터를 전송합니다.
        /// </summary>
        /// <param name="packet">packet.Token에 담겨있는 플레이어와 같은 팀인 유저끼리만 전송합니다.</param>
        public UniTask SendPacketForTeam(Packet packet)
        {
            var token = packet.Token ?? "";
            packet.Token = "";
            if (players.ContainsKey(token))
            {
                var team = players[token].Team;
                foreach (var player in players.Values)
                {
                    if (player.Team == team && TcpServer.Instance.TryGetClient(player.ClientId, out var clientHandler))
                    {
                        clientHandler.EnqueueSend(packet);
                    }
                }
            }
            else
            {
                Logger.LogError($"{token} is unknown user");
            }
            return UniTask.CompletedTask;

        }

        /// <summary>
        /// 해당 패킷에 등록된 플레이어와 같은 토큰인 유저에게 데이터를 전송합니다.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public UniTask SendPacketForUser(Packet packet)
        {
            var token = packet.Token ?? "";
            packet.Token = "";
            if (players.TryGetValue(token, out var player))
            {
                if (TcpServer.Instance.TryGetClient(player.ClientId, out var clientHandler))
                {
                    clientHandler.EnqueueSend(packet);
                }
                else
                {
                    Logger.LogError($"Unknown client : {player.ClientId}");
                }
            }
            else
            {
                Logger.LogError($"Unknown player token : {token}");
            }
            return UniTask.CompletedTask;
        }





    }
}
