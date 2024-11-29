using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Linq;
using static UnityEditor.Experimental.GraphView.GraphView;
using Protocol;
using UnityEngine.UIElements;

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
            UpdatePlayerSendTime = 4,
            UpdatePlayerReceiveTime = 5,
            PlayerSync = 6,
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
            for (int i = 0; i < teams.Length; i++)
            {
                teams[i] = new Team();
            }
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
            sendDispatcher.Subscribe(GameManagerEvent.UpdatePlayerSendTime, UpdatePlayerSendTime);

            var receivedDispatcher  = EventDispatcher<GameManagerEvent, ReceivedPacket>.Instance;
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);
            receivedDispatcher.Subscribe(GameManagerEvent.UpdatePlayerReceiveTime, UpdatePlayerReceiveTime);
            receivedDispatcher.Subscribe(GameManagerEvent.PlayerSync, PlayerSync);


        }

        public UniTask RegisterPlayer(ReceivedPacket receivedPacket)
        {
            var token = receivedPacket.Token;
            var clientId = receivedPacket.ClientId;

            if (teams.Any(t => t.ContainsPlayer(token)))
            {
                throw new System.Exception($"Already exsits player[{clientId}] : {token}");
            }
            var player = new Player(clientId);
            teams[0].Players.Add(token, player);
            playerCount++;
            Logger.Log($"Add New Player[{clientId}] : {token}");
            return UniTask.CompletedTask;
        }

        public async UniTask PlayerSync(ReceivedPacket receivedPacket)
        {
            Protocol.Position position = new();
            if (receivedPacket.Payload is C2SPlayerMove playerMove)
            {
                position = playerMove.Position;
            }
            await PlayerSyncNotify((uint)receivedPacket.ClientId, 1, PositionExtensions.ToVector3(position).normalized, 1, receivedPacket.Token);
            return;
        }

        private UniTask PlayerSyncNotify(uint ClientId, byte ActionType, Vector3 position, uint EquippedItem, string token)
        {
            Logger.Log($"기존 포지션은 : {position}");
            var protoPlayerState = new Protocol.PlayerState
            {
                PlayerId = ClientId,
                ActionType = ActionType,
                Position = Correction(position, token),
                EquippedItem = EquippedItem
            };

            var packet = new Packet
            {
                PacketType = PacketType.SYNC_PLAYERS_NOTIFICATION,

                //Token = "", // 특정 클라이언트에게 전송 시 설정
                Payload = new Protocol.S2CSyncPlayersNoti
                {
                    PlayerStates = { protoPlayerState },
                }
            };
            Logger.Log(packet);
            SendPacketForAll(packet);
            return UniTask.CompletedTask;
        }

        public UniTask UpdatePlayerSendTime(Packet packet)
        {
            if (packet.Payload is S2CPingReq pingpacket)
            {
                FindPlayer(packet.Token).lastSendTime = pingpacket.ServerTime;
            }
            
            return UniTask.CompletedTask;
        }

        public UniTask UpdatePlayerReceiveTime(ReceivedPacket receivedPacket)
        {
            if (receivedPacket.Payload is C2SPongRes pingpacket)
            {
                // 밑에 함수는 lastSendTime만 전달에서 플레이어 안에서 처리를 한다
                FindPlayer(receivedPacket.Token).LatencyCheck(pingpacket.ClientTime - FindPlayer(receivedPacket.Token).lastSendTime);
                Logger.Log($"플레이어 레이턴시 : {FindPlayer(receivedPacket.Token).playerLatency}");
            }
            
            return UniTask.CompletedTask;
        }

        public Protocol.Position Correction(Vector3 position, string token)
        {
            //속도 검사하는 로직이 빠져있고
            //이동 가능한 위치인지도 빠져있다.
            Logger.Log($"스피드는 : {FindPlayer(token).playerSpeed}, 레이턴시는 : {FindPlayer(token).playerLatency}");
            // 밑에 함수는 포지션만 전달에서 플레이어 안에서 처리를 한다
            FindPlayer(token).ChangePosition(FindPlayer(token).playerLatency * FindPlayer(token).playerSpeed * position / 1000);
            Logger.Log($"움직인 결과는 : {FindPlayer(token).position}, 토큰은 : {token}");
            return position.FromVector();
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
