using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Linq;
using Protocol;

namespace ResourceWar.Server
{
    /// <summary>
    /// 게임 관리 클래스.
    /// 게임 상태 관리 및 플레이어 등록, 데이터 전송 등의 주요 기능을 담당.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
       

        // GameManager에서 처리하느 주요 이벤트를 정의하는 열거형
        public enum GameManagerEvent
        {
            SendPacketForAll = 0,
            SendPacketForTeam = 1,
            SendPacketForUser = 2,
            AddNewPlayer = 3,
            ClientRemove = 4,
            PlayerSync = 5,
        }

        public GameSessionState GameState;
        // 게임의 고유 토큰 (서버가 게임 세션을 식별하기 위해 사용)
        public static string GameCode;
        // 이벤트 구독 여부를 확인하는 플래그
        private bool subscribed = false;

        /// <summary>
        /// 0 - Gray(팀 선택x), 1 - Blue, 2 - Red
        /// </summary>
        private Team[] teams = null;
        // 현재 등록된 플레이어 수
        private int playerCount = 0;

        private GameSessionInfo gameSessionInfo = new GameSessionInfo();

        private void Awake()
        {
            Logger.Log($"{nameof(GameManager)}:{GameCode} is Awake");
            _ = Init();
        }
        public async UniTaskVoid Init()
        {

            /* [최초 초기화]
             * 1. 레디스에 대기중인 게임 서버 목록에 등록
             * 2. 모든 초기화가 완료 되었을 경우 점유 알림 채널에 구독
             * 
             * [재활용]
             * 1. 기존 게임코드에 대한 레디스 정보를 말소 해야함
             * 2. 기존 게임 매니저 내 정보 초기화
             * 3. 다시 레디스에 대기중인 게임 서버 목록에 등록
             */
            bool reinit = string.IsNullOrWhiteSpace(GameCode);
            if (reinit)
            {
                await GameRedis.RemoveGameSessionInfo(GameCode);
                //새로 생성된 방으로서 초기화 작업
                gameSessionInfo.state = GameSessionState.CREATING;
                gameSessionInfo.currentPlayer = 0;
                gameSessionInfo.maxPlayer = 4;
                gameSessionInfo.previousPlayer = 0;
                gameSessionInfo.roomMaster = string.Empty;
                gameSessionInfo.createdAt = UnixTime.Now();
                gameSessionInfo.updatedAt = gameSessionInfo.createdAt;
                gameSessionInfo.gameUrl = string.Empty;
                foreach (Team team in teams)
                {
                    team.Reset();
                }
            }
            else
            {
                teams = new Team[3];
                for (int i = 0; i < teams.Length; i++)
                {
                    teams[i] = new Team();
                }
            }
            await GameRedis.AddGameSessionInfo(GameCode, gameSessionInfo);
            
            Subscribes();
            await SetState(GameSessionState.LOBBY);
        }

        
        /// <summary>
        /// 게임 상태 변경 및 Redis 서버에 상태 저장
        /// </summary>
        /// <param name="state">변경할 게임 상태</param>
        /// <returns></returns>
        public async UniTask SetState(GameSessionState state)
        {
            if (this.gameSessionInfo.state != state)
            {
                await GameRedis.SetGameState(GameCode,state);
            }
            this.gameSessionInfo.state = state;
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

            var receivedDispatcher  = EventDispatcher<GameManagerEvent, ReceivedPacket>.Instance;
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);
            receivedDispatcher.Subscribe(GameManagerEvent.PlayerSync, PlayerSync);

            var innterDispatcher = EventDispatcher<GameManagerEvent, int>.Instance;
            innterDispatcher.Subscribe(GameManagerEvent.ClientRemove, ClientRemove);
        }

        public async UniTask PlayerSync(ReceivedPacket receivedPacket)
        {
            Protocol.Position position = new();
            if (receivedPacket.Payload is C2SPlayerMove playerMove)
            {
                position = playerMove.Position;
            }
            await PlayerSyncNotify((uint)receivedPacket.ClientId, 1, position.ToVector3(), 1, receivedPacket.Token);
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

        public Protocol.Position Correction(Vector3 position, string token)
        {
            //속도 검사하는 로직이 빠져있고
            //이동 가능한 위치인지도 빠져있다.
            // 밑에 함수는 포지션만 전달에서 플레이어 안에서 처리를 한다
            FindPlayer(token).ChangePosition(position);
            return position.FromVector();

        }

        /// <summary>
        /// 클라이언트 제거 처리.
        /// </summary>
        /// <param name="clientId">제거할 클라이언트 ID</param>
        public async UniTask ClientRemove(int clientId)
        {
            if (GameState == GameSessionState.LOBBY)
            {
               // await PlayerRedis.RemovePlayerInfo(GameToken, clientId);
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


        private bool TryGetPlayer(int clientId, out Player player)
        {
            foreach (var team in teams)
            {
                foreach(var teamPlayer in team.Players.Values)
                {
                    if(teamPlayer.ClientId == clientId)
                    {
                        player = teamPlayer;
                        return true;
                    }
                }
            }
            player = null;
            return false;
        }

        private bool TryGetPlayer(string token, out Player player)
        {
            foreach (var team in teams)
            {
                if (team.Players.ContainsKey(token))
                {
                    player = team.Players[token];
                    return true;
                }
            }

            player = null;
            return false;
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
                gameToken: GameCode,
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

        /// <summary>
        /// 모든 유저에게 현재 방 상태를 알림.
        /// </summary>
        public async UniTask NotifyRoomState()
        {
            var players = await PlayerRedis.GetAllPlayersInfo(GameCode);

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
