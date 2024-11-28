using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Linq;
using Protocol;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;

namespace ResourceWar.Server
{
    /// <summary>
    /// 게임 관리 클래스.
    /// 게임 상태 관리 및 플레이어 등록, 데이터 전송 등의 주요 기능을 담당.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum State : int
        {
            CREATING = 0,
            DESTROY,
            LOBBY,
            LOADING,
            PLAYING
        }

        // GameManager에서 처리하느 주요 이벤트를 정의하는 열거형
        public enum GameManagerEvent
        {
            SendPacketForAll = 0,
            SendPacketForTeam = 1,
            SendPacketForUser = 2,
            AddNewPlayer = 3,
            ClientRemove = 4,
        }

        // 현재 게임 상태를 저장
        public State GameState { get; private set; } = State.CREATING;
        // 게임의 고유 토큰 (서버가 게임 세션을 식별하기 위해 사용)
        public string GameToken { get; private set; }
        // 이벤트 구독 여부를 확인하는 플래그
        private bool subscribed = false;

        /// <summary>
        /// 0 - Gray(팀 선택x), 1 - Blue, 2 - Red
        /// </summary>
        private Team[] teams = null;
        // 현재 등록된 플레이어 수
        private int playerCount = 0;

        private void Awake()
        {
            Logger.Log($"{nameof(GameManager)} is Awake");
            _ = Init();
        }
        public async UniTaskVoid Init()
        {
      
            teams = new Team[3];
            for (int i = 0; i < teams.Length; i++)
            {
                teams[i] = new Team();
            }
            Subscribes();
            await SetState(State.LOBBY);
        }

        /// <summary>
        /// 게임 상태 변경 및 Redis 서버에 상태 저장
        /// </summary>
        /// <param name="state">변경할 게임 상태</param>
        /// <returns></returns>
        public async UniTask SetState(State state)
        {
            this.GameState = state;
            await GameRedis.SetGameState(state);
        }

        /// <summary>
        /// 이벤트에 대한 구독 설정
        /// </summary>
        private void Subscribes()
        {
            // 재구독 방지
            if (subscribed)
            {
                return;
            }
            subscribed = true;
            
            // 패킷 전송 관련 이벤트 등록
            var sendDispatcher = EventDispatcher<GameManagerEvent, Packet>.Instance;
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForAll, SendPacketForAll);
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForTeam, SendPacketForTeam);
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForUser, SendPacketForUser);
             
            // 플레이어 등록 관련이벤트 등록
            var receivedDispatcher  = EventDispatcher<GameManagerEvent, ReceivedPacket>.Instance;
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);

            //
            var innterDispatcher = EventDispatcher<GameManagerEvent, int>.Instance;
            innterDispatcher.Subscribe(GameManagerEvent.ClientRemove, ClientRemove);
        }

        public async UniTask ClientRemove(int clientId)
        {
            if (GameState == State.LOBBY)
            {
                await PlayerRedis.RemovePlayerInfo(GameToken, clientId);
                // 현재 플레이어 목록에서도 날려야함
                foreach (var team in teams)
                {
                    var playerToRemove = team.Players.FirstOrDefault(p => p.Value.ClientId == clientId);
                    if (!playerToRemove.Equals(default(KeyValuePair<string, Player>)))
                    {
                        team.Players.Remove(playerToRemove.Key);
                        playerCount--;
                        Logger.Log($"플레이어 {clientId}가 팀에서 제거되었습니다.");
                        break;
                    }
                }
            }
            else
            {

            }

            await NotifyRoomState();
        }

        /// <summary>
        /// 새로운 플레이어 등록
        /// </summary>
        /// <param name="receivedPacket">등록 요청 데이터를 포함한 패킷</param>
        /// <returns></returns>
        public async UniTask RegisterPlayer(ReceivedPacket receivedPacket)
        {
            if (playerCount >= 4)
            {
                throw new System.InvalidOperationException("Player count has reached its maximum limit.");
            }

            var token = receivedPacket.Token;
            var clientId = receivedPacket.ClientId;
            if (teams.Any(t => t.ContainsPlayer(token)))
            {
                throw new System.InvalidOperationException($"Already exists player[{clientId}] : {token}");
            }

            // 플레이어 객체 생성 및 팀 0에 추가
            var player = new Player(clientId);
            teams[0].Players.Add(token, player);
            playerCount++;
            Logger.Log($"Add New Player[{clientId}] : {token}");

            // 이름을 가져와야하는데 어디서 가져올지 고민중
            var userName = await UserRedis.GetNickName(token);
            // Redis에 플레이어 정보 저장
            // AratarI는 어딘가에서 가져와야하는데 아직 모름
            await PlayerRedis.AddPlayerInfo(
                gameToken: GameToken,
                clientId: clientId,
                userName: userName,
                isReady: false, // Default values
                connected: true,
                loadProgress: 0,
                teamId: 0,
                avatarId: 1
            );

            // Notify all players in the same lobby
            await NotifyRoomState();
        }

        public async UniTask NotifyRoomState()
        {
            var players = await PlayerRedis.GetAllPlayersInfo(GameToken);

            var syncRoomNoti = new S2CSyncRoomNoti
            {
                Players =
                {
                    players.Select(p => new PlayerRoomInfo
                    {
                        PlayerId = (uint)p.ClientId,
                        PlayerName = p.UserName,
                        AvartarItem = (uint)p.AvatarId,
                        TeamIndex = (uint)p.TeamId,
                        Ready = p.IsReady,
                    })                   
                }
            };

            var packet = new Packet
            {
                PacketType = PacketType.SYNC_ROOM_NOTIFICATION,
                Token = "",
                Payload = syncRoomNoti
            };

            await SendPacketForAll(packet);
        }

        /// <summary>
        /// 플레이어 검색
        /// </summary>
        /// <param name="token">플레이어의 고유 토큰</param>
        /// <returns>찾은 플레이어 객체 or Null</returns>
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
                   return UniTask.FromException(new System.InvalidOperationException( $"Unknown client : {player.ClientId}"));
                }
            }
            else
            {
                return UniTask.FromException(new System.InvalidOperationException($"Unknown player token : {token}"));
            }
            return UniTask.CompletedTask;
        }
    }
}
