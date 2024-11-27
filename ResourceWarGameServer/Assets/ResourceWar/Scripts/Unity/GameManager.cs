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
        }

        public State GameState { get; private set; } = State.CREATING;
        public string GameToken { get; private set; }
        private bool subscribed = false;

        private List<Team> teams = new List<Team>();


        public async UniTaskVoid Init()
        {
            GameState = State.CREATING;
            teams.Clear();
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

        public void RegisterPlayer(string token, int clientId)
        {
            // 기존 팀 목록에서 플레이어가 등록될 팀을 찾기
            foreach (var team in teams)
            {
                if (!team.ContainsPlayer(token)) // 해당 팀에 플레이어가 없다면
                {
                    // 새로운 Player 객체 생성
                    var player = new Player(clientId: clientId);  // 예시로 clientId는 123으로 설정, 실제로는 token에 맞는 값을 사용

                    // 플레이어를 팀에 추가
                    team.Players[token] = player;  // Players 딕셔너리에 token을 key로 플레이어를 추가

                    Logger.Log($"Player with token {token} added to an existing team.");
                    return;
                }
            }

            // 만약 모든 팀에 플레이어가 없으면 새로운 팀을 만들어서 추가
            var newTeam = new Team();
            var newPlayer = new Player(clientId: clientId); // player의 clientId는 실제로 필요에 따라 설정

            newTeam.Players[token] = newPlayer;  // 새 팀에 플레이어 추가
            teams.Add(newTeam);  // 팀 목록에 새 팀 추가

            Logger.Log($"New team created and player with token {token} added.");
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
