using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Linq;
using static UnityEditor.Experimental.GraphView.GraphView;

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
            AddNewPlayer = 3,
        }

        public State GameState { get; private set; } = State.CREATING;
        public string GameToken { get; private set; }
        private bool subscribed = false;

        /// <summary>
        /// 0 - Gray(팀 선택x), 1 - Blue, 2 - Red
        /// </summary>
        private Team[] teams = null;
        private int playerCount = 0;

        private void Awake()
        {
            Logger.Log($"{nameof(GameManager)} is Awake");
            _ = Init();
        }

        public async UniTaskVoid Init()
        {
            GameState = State.CREATING;
            teams = new Team[3];
            teams[0] = new Team(0); 
            teams[1] = new Team(1); // Blue Team
            teams[2] = new Team(2); // Red Team
            Subscribes();
        }

        private void Subscribes()
        {
            if (subscribed)
            {
                return;
            }
            subscribed = true;
            
            var sendDispatcher = EventDispatcher<GameManagerEvent, Packet>.Instance;
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForAll, SendPacketForAll);
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForTeam, SendPacketForTeam);
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForUser, SendPacketForUser);
            
            var receivedDispatcher  = EventDispatcher<GameManagerEvent, ReceivedPacket>.Instance;
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);


        }

        public UniTask RegisterPlayer(ReceivedPacket receivedPacket)
        {
            var token = receivedPacket.Token;
            var clientId = receivedPacket.ClientId;

            if(teams.Any(t => t.ContainsPlayer(token)))
            {
                throw new System.Exception($"Already exsits player[{clientId}] : {token}");
            }
            var player = new Player(clientId);
            teams[0].Players.Add(token, player);
            playerCount++;
            Logger.Log($"Add New Player[{clientId}] : {token}");
            return UniTask.CompletedTask;
        }


        public Player FindPlayer(string token)
        {
            foreach (var team in teams)
            {
                if (team.Players.TryGetValue(token, out Player player)) return player;
            }
            return null;
        }

        /// <summary>
        /// 게임 내 모든 유저에게 데이터를 보냅니다.
        /// </summary>
        /// <param name="packet"></param>
        public UniTask SendPacketForAll(Packet packet)
        {
            packet.Token = "";

            foreach (var team in teams)
            {
                foreach (var player in team.Players.Values)
                {
                    if (TcpServer.Instance.TryGetClient(player.ClientId, out var clientHandler))
                    {
                        clientHandler.EnqueueSend(packet);
                    }
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
            if (teams.Any(a => a.ContainsPlayer(token)))
            {
                foreach (var team in teams)
                {
                    foreach (var player in team.Players.Values)
                    {
                        if (TcpServer.Instance.TryGetClient(player.ClientId, out var clientHandler))
                        {
                            clientHandler.EnqueueSend(packet);
                        }
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
            var player = FindPlayer(token);

            if (player != null)
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
