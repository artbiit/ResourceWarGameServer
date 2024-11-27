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
        // 팀 ID를 키로 하는 용광로 Dictionary
        private Dictionary<int, FurnaceClass> furnaces = new Dictionary<int, FurnaceClass>();

        #region 용광로
        /// <summary>
        /// 특정 팀(TeamId)에 해당하는 용광로를 등록합니다.
        /// </summary>
        /// <param name="teamId">팀 ID</param>
        /// <param name="furnace">등록할 용광로 객체</param>
        public void RegisterFurnace(int teamId, FurnaceClass furnace)
        {
            if (!furnaces.ContainsKey(teamId))
            {
                furnaces[teamId] = furnace;
                Logger.Log($"Furnace registered for TeamId {teamId}");
            }
            else
            {
                Logger.LogError($"Furnace for TeamId {teamId} already exists.");
            }
        }

        /// <summary>
        /// 특정 팀(TeamId)에 해당하는 용광로를 반환합니다.
        /// </summary>
        /// <param name="teamId">팀 ID</param>
        /// <returns>FurnaceClass 객체</returns>
        public FurnaceClass GetFurnaceByTeamId(int teamId)
        {
            if (furnaces.TryGetValue(teamId, out var furnace))
            {
                return furnace;
            }

            Logger.LogError($"Furnace not found for TeamId {teamId}");
            return null;
        }
        #endregion

        public async UniTaskVoid Init()
        {
            furnaces.Clear();
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
