using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Linq;
using Protocol;
using Google.Protobuf.Collections;

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
            QuitLobby = 5,
            PlayerSync = 6,
            TeamChange = 7,
            PlayerIsReadyChanger =8,
            GameStart = 9,
            LoadProgressNoti = 10,
            SurrenderNoti = 11,
            FurnaceHandler = 12,

        }

        public GameSessionState GameState => gameSessionInfo.state;
        // 게임의 고유 토큰 (서버가 게임 세션을 식별하기 위해 사용)
        public static string GameCode {get; private set;}

        /// <summary>
        /// 0 - Gray(팀 선택x), 1 - Blue, 2 - Red
        /// </summary>
        private Team[] teams = null;
        // 현재 등록된 플레이어 수
        private int _playerCount = 0;
        private int playerCount
        {
            get { return _playerCount; }
            set { 
                _playerCount = value;
                _ = GameRedis.SetCurrentPlayerCount(GameCode, value);
            }
        }
        private GameSessionInfo gameSessionInfo = new GameSessionInfo();

        private TimerManager<int> timerManager = new TimerManager<int>();

        private void Awake()
        {
            Logger.Log($"{nameof(GameManager)}:{GameCode} is Awake");
            _ = Init();
        }
        public async UniTaskVoid Init()
        {

            /* [최초 초기화]
             * 1. 레디스에 대기중인 게임 서버 목록에 등록
             * 2. 모든 초기화가 완료 되었을 경우 점유 알림 채널에 송신
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
                GenerateGameCode();
            }
            else
            {
                teams = new Team[3];
                for (int i = 0; i < teams.Length; i++)
                {
                    teams[i] = new Team();
                }
                Provides();
                Subscribes();
            }

            await GameRedis.AddGameSessionInfo(GameCode, gameSessionInfo);
            await SetState(GameSessionState.LOBBY);
        }

        public static string GenerateGameCode()
        {
            GameCode = NanoidDotNet.Nanoid.Generate(size: 11);
            return GameCode;
        }

        /// <summary>
        /// 게임 상태 변경 및 Redis 서버에 상태 저장
        /// </summary>
        /// <param name="state">변경할 게임 상태</param>
        /// <returns></returns>
        public async UniTask SetState(GameSessionState state)
        {
            this.gameSessionInfo.state = state;
            await GameRedis.SetGameState(GameCode,state);
   
        }


        /// <summary>
        /// 제공할 데이터들 구독 설정
        /// </summary>
        private void Provides()
        {
            DataDispatcher<int, Player>.Instance.SetProvider((clientId) =>
            {
                if (TryGetPlayer(clientId, out var player))
                {
                    return UniTask.FromResult(player);
                }
                return UniTask.FromResult<Player>(null);
            });

            DataDispatcher<int, ( int teamIndex, Team team)>.Instance.SetProvider((teamId) => { 
                if(TryGetTeamIndex(teamId,out var teamIndex, out var team))
                {
                    return UniTask.FromResult((teamIndex,team));
                }
                return UniTask.FromResult<(int teamIndex, Team team)>((-1,null));
            });

            DataDispatcher<int, Furnace>.Instance.SetProvider((teamId) =>
            {
                if (TryGetTeam(teamId, out var team))
                {
                    return UniTask.FromResult(team.TeamFurnace);
                }
                return UniTask.FromResult<Furnace>(null);
            });

        }

        /// <summary>
        /// 이벤트에 대한 구독 설정
        /// </summary>
        private void Subscribes()
        {
            // 패킷 전송 관련 이벤트 등록
            var sendDispatcher = EventDispatcher<GameManagerEvent, Packet>.Instance;
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForAll, SendPacketForAll);
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForTeam, SendPacketForTeam);
            sendDispatcher.Subscribe(GameManagerEvent.SendPacketForUser, SendPacketForUser);

            // 플레이어 등록 관련이벤트 등록
            var receivedDispatcher = EventDispatcher<GameManagerEvent, ReceivedPacket>.Instance;
            receivedDispatcher.Subscribe(GameManagerEvent.QuitLobby, QuitLobby);
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);
            receivedDispatcher.Subscribe(GameManagerEvent.PlayerSync, PlayerSync);
            receivedDispatcher.Subscribe(GameManagerEvent.TeamChange, TeamChange);
            receivedDispatcher.Subscribe(GameManagerEvent.PlayerIsReadyChanger, PlayerReadyStateChanger);
            receivedDispatcher.Subscribe(GameManagerEvent.GameStart, GameStart);
            receivedDispatcher.Subscribe(GameManagerEvent.LoadProgressNoti, LoadProgressNoti);
            receivedDispatcher.Subscribe(GameManagerEvent.SurrenderNoti, SurrenderNoti);

            //
            var innerDispatcher = EventDispatcher<GameManagerEvent, int>.Instance;
            innerDispatcher.Subscribe(GameManagerEvent.ClientRemove, ClientRemove);
        }

        #region 플레이어 상태 동기화
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
        #endregion

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
        /// 팀 목록에서도 제거
        /// </summary>
        /// <param name="clientId">제거할 클라이언트 ID</param>
        public async UniTask ClientRemove(int clientId)
        {
            if (GameState == GameSessionState.LOBBY)
            {
                if (TryGetTeam(clientId, out Team team))
                {
                   if(team.TryRemoveByClient(clientId))
                    {
                        //인메모리 팀에서 플레이어 제거
                        playerCount--;
                        Logger.Log($"플레이어 {clientId}가 팀에서 제거되었습니다.");
                    }
                    else
                    {
                        Logger.LogError($"ClientRemove. Could not found player[{clientId}] in team");
                    }
                }
                else
                {
                    Logger.LogWarning($"플레이어 {clientId}를 찾을 수 없습니다.");
                }
            }
            else
            {
                Logger.Log($"현재 상태가 LOBBY가 아니므로 제거 작업이 무시되었습니다. 현재 상태: {GameState}");
            }

            if (!IsRemainedTeamPlayers())
            {
                // 해당 GameSession파괴
            }
            await NotifyRoomState();
        }
        #region Find Player Or Team
        /// <summary>
        /// TeamIndex, UserToken, Player 매개 변수로 순환함
        /// </summary>
        /// <param name="action"></param>
        public void LoopAllPlayers(System.Action<int, string, Player> action)
        {

            for (int i = 0; i < teams.Length; i++)
            {
                var team = teams[i];
                foreach (var playerPair in team.Players)
                {
                    action?.Invoke(i, playerPair.Key, playerPair.Value);
                }
            }
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

        private bool TryGetPlayer(int clientId, out Player player)
        {
            foreach (var team in teams)
            {
                foreach (var teamPlayer in team.Players.Values)
                {
                    if (teamPlayer.ClientId == clientId)
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
        /// Teams에 Player가 한명이라도 남아 있는지 체크
        /// </summary>
        /// <returns></returns>
        private bool IsRemainedTeamPlayers()
        {
            foreach (var team in teams)
            {
                if (team.HasPlayers())
                {
                    Logger.Log($"Team에 플레이어가 남아 있습니다.");
                    return true;
                }
            }

            Logger.Log("모든 팀에서 플레이어가 제거되었습니다.");
            return false;
        }

        private bool TryGetTeam(int clinetId, out Team team)
        {
            foreach (var currentTeam in teams)
            {
                foreach (var player in currentTeam.Players.Values)
                {
                    if (player.ClientId == clinetId)
                    {
                        team = currentTeam;
                        return true;
                    }
                }
            }

            team = null;
            return false;
        }

        private bool TryGetChangeTeamForPlayer(string token, int changeTeamIndex)
        {
            if (changeTeamIndex < 0 || changeTeamIndex >= teams.Length)
            {
                Logger.LogError($"Invalid team index: {changeTeamIndex}");
                return false;
            }

            var player = FindPlayer(token);
            if (player == null)
            {
                Logger.LogError($"Player with token {token} not found");
                return false;
            }

            var currentTeamIndex = GetPlayerTeamIndex(player.ClientId);
            if (currentTeamIndex == null)
            {
                Logger.LogError($"Player {player.ClientId} is not assigned to any team.");
                return false;
            }

            if (MovePlayerBetweeonTeams(token, currentTeamIndex.Value, changeTeamIndex))
            {
                Logger.Log($"Player {player.ClientId} successfully moved from team {currentTeamIndex} to team {changeTeamIndex}.");
                return true;
            }

            Logger.LogError($"Failed to move player {player.ClientId} to team {changeTeamIndex}.");
            return false;
        }

        /// <summary>
        /// 플레이어를 팀 간 이동시키는 메서드
        /// </summary>
        private bool MovePlayerBetweeonTeams(string token, int fromTeamIndex, int toTeamIndex)
        {
            if (teams[fromTeamIndex].Players.TryGetValue(token, out var player))
            {
                teams[fromTeamIndex].Players.Remove(token);
                teams[toTeamIndex].Players.Add(token, player);
                return true;
            }
            return false;
        }

        public int? GetPlayerTeamIndex(int clientId)
        {

            for (int i = 0; i < teams.Length; i++)
            {
                foreach (var player in teams[i].Players.Values)
                {
                    if (player.ClientId == clientId)
                    {
                        Logger.Log($"Player[{clientId})는 Team[{i}]에 속해 있습니다.");
                        return i;
                    }
                }
            }

            Logger.LogWarning($"Player[{clientId}]를 찾을 수 없습니다.");
            return null;
        }

        private bool TryGetTeamIndex(int clientId, out int teamIndex, out Team team)
        {
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i].ContainsPlayer(clientId))
                {
                    teamIndex = i;
                    team = teams[i];
                    return true;
                }
            }

            teamIndex = -1;
            team = null;
            return false;
        } 
        #endregion


        #region 항복 투표
        private Dictionary<int, HashSet<int>> surrenderVotes = new Dictionary<int, HashSet<int>>();
        // 팀 별 항복 투표 상태를 저장하는 데이터 구조. Key: 팀 Index, Value: 투표한 Player ID 목록

        public async UniTask SurrenderNoti(ReceivedPacket receivedPacket)
        {
            var clientId = receivedPacket.ClientId;
            if (!TryGetTeamIndex(clientId, out int teamIndex, out var team))
            {
                Logger.Log($"Player {clientId}가 속한 팀을 찾을 수 없습니다.");
                return;
            }

            if (!surrenderVotes.ContainsKey(teamIndex))
            {
                surrenderVotes[teamIndex] = new HashSet<int>();


                // 이미 타이머가 실행 중인지 확인
                if (!timerManager.IsTimerActive(teamIndex))
                {
                    // 타이머 시작
                    timerManager.StartTimer(teamIndex, 180, OnSurrenderVoteTimeout);
                }
            }

            var votes = surrenderVotes[teamIndex];
            if (votes.Contains(clientId))
            {
                Logger.LogError($"Player {clientId}는 이미 항복 투표에 참여했습니다.");
                return;
            }

            votes.Add(clientId);
            Logger.Log($"Player {clientId}가 팀 {teamIndex}의 항복 투표에 참여했습니다. 현재 투표 수: {votes.Count}/{team.Players.Count}");

            // 투표 상태를 알림
            var packet = new Packet
            {
                PacketType = PacketType.SURRENDER_NOTIFICATION,
                Token = receivedPacket.Token,
                Payload = new S2CSurrenderNoti
                {
                    PlayerId = (uint)clientId,
                    IsSurrender = true,
                    SurrenderStartTime = (ulong)UnixTime.Now()
                }
            };

            await SendPacketForTeam(packet);
            
            // 과반수 체크
            if (votes.Count > team.Players.Count / 2)
            {
                Logger.Log($"팀 {teamIndex}의 항복이 승인되었습니다.");
                await HandleSurrender(teamIndex);
            }
        }

        /// <summary>
        /// 투표 시간이 초과되었을 때 실행되는 콜백.
        /// </summary>
        /// <param name="teamIndex"></param>
        private void OnSurrenderVoteTimeout(int teamIndex)
        {
            if (surrenderVotes.ContainsKey(teamIndex))
            {
                surrenderVotes.Remove(teamIndex); // 투표 삭제
                Logger.Log($"팀 {teamIndex}의 항복 투표 데이터가 시간 초과로 제거되었습니다.");

                // 필요하다면 시간 초과 알림 패킷 전송
                /*var packet = new Packet
                {
                    PacketType = PacketType.SURRENDER_TIMEOUT_NOTIFICATION,
                    Payload = new S2CSurrenderTimeoutNoti
                    {
                        TeamIndex = (uint)teamIndex,
                    }
                };

                SendPacketForTeam(packet).Forget();*/
            }
        }
    
        /// <summary>
        /// GameOver만들면 마무리 예정
        /// </summary>
        /// <param name="teamIndex"></param>
        /// <returns></returns>
        private async UniTask HandleSurrender(int teamIndex)
        {
            Logger.Log($"팀 {teamIndex} 항복 처리 중...");

            // 항복 로직 처리
            gameSessionInfo.state = GameSessionState.GAMEOVER; // 게임 상태 변경

            surrenderVotes.Remove(teamIndex);

            // 항복 결과를 모든 클라이언트에게 알림
            // 여기서 message S2CGameOverNoti에 관한걸 넣어주면 될듯

            Logger.Log("게임이 종료되었습니다.");
        }
        #endregion

        /// <summary>
        /// 새로운 플레이어 등록
        /// 해결 완료
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

            var nickName = await UserRedis.GetNickName(token);
            if (string.IsNullOrEmpty(nickName))
            {
                throw new System.InvalidOperationException($"Invalid nickName for token: {token}");
            }


            // 플레이어 객체 생성 및 팀 0에 추가
            var player = new Player(clientId, nickName);
            teams[0].Players.Add(token, player);
            playerCount++;

            Logger.Log($"Add New Player[{clientId}] : {token}");


            player.Nickname = nickName;

            // Notify all players in the same lobby
            await NotifyRoomState();
        }

        // Handler에서 호출하는 팀 변경
        public async UniTask TeamChange(ReceivedPacket receivedPacket)
        {
            var teamChangeMessage = (C2STeamChangeReq)receivedPacket.Payload;
            if (teamChangeMessage == null)
            {
                Logger.LogError("Invalid TeamChange message payload.");
                return;
            }

            var token = receivedPacket.Token;
            var requestTeamIndexMessage = (int)teamChangeMessage.TeamIndex;

            if (!TryGetChangeTeamForPlayer(token, requestTeamIndexMessage))
            {
                Logger.LogError($"Failed to change team for player with token {token} to team {requestTeamIndexMessage}.");
                return;
            }

            // 팀 변경 후 방 상태 알림
            Logger.Log($"Player {token} successfully changed to team {requestTeamIndexMessage}.");
            await NotifyRoomState();
        }

        // 해당 token 또는 clientId로 Player를 찾고 그 플레이어의 isReady 상태를 바꿔주기.
        public async UniTask PlayerReadyStateChanger(ReceivedPacket receivedPacket)
        {
            var token = receivedPacket.Token;
            var clientId = receivedPacket.ClientId;

            Player player = null;
            if (!TryGetPlayer(clientId, out player))
            {
                Logger.LogError($"Player with clinetId {clientId} not found");
                return;
            }

            // isReady 상태 반전
            player.IsReady = !player.IsReady;
            Logger.Log($"Player[{player.ClientId}] Ready State Change to: {player.IsReady}");

            await NotifyRoomState();
        }

        /// <summary>
        /// 방나가기 요청이 들어왔을 때 실행되는 알림
        /// </summary>
        /// <param name="receivedPacket"></param>
        public async UniTask QuitLobby(ReceivedPacket receivedPacket)
        {
            var token = receivedPacket.Token;

            var clientId = receivedPacket.ClientId;

            var quitLobby = new S2CQuitRoomNoti
            {
                PlayerId = (uint)clientId
            };

            var packet = new Packet
            {
                PacketType = PacketType.QUIT_ROOM_NOTIFICATION,
                Token = "",
                Payload = quitLobby
            };

            Logger.Log($"QUIT_ROOM_NOTIFICATION => {packet}");
            // 방 나가기했다는 Noti
            await SendPacketForAll(packet);

            // 방에서 제거하는 코드
            await ClientRemove(clientId);
        }

        /// <summary>
        /// 게임 시작
        /// </summary>
        /// <param name="receivedPacket"></param>
        /// <returns></returns>
        public async UniTask GameStart(ReceivedPacket receivedPacket)
        {
            S2CGameStartNoti s2CGameStartNoti = new S2CGameStartNoti();

            if (teams[0].Players.Count > 0)
            {
                Logger.LogError($"Game cannot start. Teams[0] has player for {teams[0].Players.Count}.");
            }

            bool isReady = true;

            for (int i = 1; i < teams.Length; i++)
            {
                if (teams[i].Players.Count != 2)
                {
                    Logger.LogError($"Game cannot start. Because of Team[{i}]");
                    isReady = false;
                    break;
                } 
                
                foreach(var player in teams[i].Players.Values)
                {
                    if(!player.IsReady)
                    {
                        i = teams.Length;
                        Logger.LogError($"Game cannot start. Player {player.ClientId} is not ready. ");
                        isReady = false;
                        break;
                    }
                }
            }
            // CPU가 분기를 예측할 수 있는 확률이 조금더 올라가요.
            if (!isReady)
            {
                return;
            }

            var packet = new Packet
            {
                PacketType = PacketType.GAME_START_NOTI,
                Token = "",
                Payload = s2CGameStartNoti
            };

            Logger.Log($"GameStart");
            await SendPacketForAll(packet);
        }

        /// <summary>
        /// 로드 진행도 바꿔서 알리기
        /// </summary>
        /// <param name="receivedPacket"></param>
        /// <returns></returns>
        public async UniTask LoadProgressNoti(ReceivedPacket receivedPacket)
        {
            var loadProgressNoti = (C2SLoadProgressNoti)receivedPacket.Payload;

            if (loadProgressNoti.Progress < 0 || loadProgressNoti.Progress > 100)
            {
                loadProgressNoti.Progress = 0;
            }

            if (!TryGetPlayer(receivedPacket.ClientId, out var player))
            {
                Logger.LogError($"Player with ClientId {receivedPacket.ClientId} not found.");
                return;
            }
            // 플레이어의 LoadProgress 값 업데이트
            player.LoadProgress = (int)loadProgressNoti.Progress;
            Logger.Log($"Player[{player.ClientId}] load progress updated to: {player.LoadProgress}%");

            await NotifySyncLoadProgress();
        }

        /// <summary>
        /// 모든 유저에게 현재 방 상태를 알림.
        /// </summary>
        public async UniTask NotifyRoomState()
        {

            S2CSyncRoomNoti s2CSyncRoomNoti = new S2CSyncRoomNoti();

            LoopAllPlayers((teamIndex, token, player) =>
            {
                s2CSyncRoomNoti.Players.Add(new PlayerRoomInfo
                {
                    PlayerId = (uint)player.ClientId,
                    PlayerName = player.Nickname,
                    AvartarItem = (uint)player.AvatarId,
                    TeamIndex = (uint)teamIndex,
                    Ready = player.IsReady,
                });

            });


            var packet = new Packet
            {
                PacketType = PacketType.SYNC_ROOM_NOTIFICATION,
                Token = "",
                Payload = s2CSyncRoomNoti
            };

            Logger.Log($"SYNC_ROOM_NOTIFICATION => {packet}");
            await SendPacketForAll(packet);
        }

        /// <summary>
        /// 모든 플레이어 아이디, Progress 정보 알림
        /// </summary>
        /// <returns></returns>
        public async UniTask NotifySyncLoadProgress()
        {
            S2CSyncLoadNoti s2CSyncLoadNoti = new S2CSyncLoadNoti();

            LoopAllPlayers((teamIndex, token, player) =>
            {
                s2CSyncLoadNoti.SyncLoadData.Add(new S2CSyncLoadNoti.Types.SyncLoadData
                {
                    PlayerId = (uint)player.ClientId,
                    Progress = (uint)player.LoadProgress,
                });
            });

            var packet = new Packet
            {
                PacketType = PacketType.SYNC_LOAD_NOTIFICATION,
                Token = "",
                Payload = s2CSyncLoadNoti
            };

            // 패킷 로그 출력
            Logger.Log($"SYNC_LOAD_NOTIFICATION => {packet}");

            await SendPacketForAll(packet);
        }

        #region Send_Packet
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
                    return UniTask.FromException(new System.InvalidOperationException($"Unknown client : {player.ClientId}"));
                }
            }
            else
            {
                return UniTask.FromException(new System.InvalidOperationException($"Unknown player token : {token}"));
            }
            return UniTask.CompletedTask;
        }
        #endregion

        private async void OnDestroy()
        {
           await GameRedis.RemoveGameSessionInfo(GameCode);
        }
    }
}
