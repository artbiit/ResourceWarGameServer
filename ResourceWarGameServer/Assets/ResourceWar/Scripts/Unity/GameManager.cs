using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Linq;
using Protocol;
using Google.Protobuf.Collections;
using UnityEngine.UIElements;
using System.ComponentModel;

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
            QuitLobby = 5,
            PlayerSync = 6,
            TeamChange = 7,
            PlayerIsReadyChanger =8,

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

            GameState = State.CREATING;
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
            var receivedDispatcher = EventDispatcher<GameManagerEvent, ReceivedPacket>.Instance;
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);
            receivedDispatcher.Subscribe(GameManagerEvent.QuitLobby, QuitLobby);
            receivedDispatcher.Subscribe(GameManagerEvent.AddNewPlayer, RegisterPlayer);
            receivedDispatcher.Subscribe(GameManagerEvent.PlayerSync, PlayerSync);
            receivedDispatcher.Subscribe(GameManagerEvent.TeamChange, TeamChange);
            receivedDispatcher.Subscribe(GameManagerEvent.PlayerIsReadyChanger, PlayerReadyStateChanger);
            
            //
            var innerDispatcher = EventDispatcher<GameManagerEvent, int>.Instance;
            innerDispatcher.Subscribe(GameManagerEvent.ClientRemove, ClientRemove);
        }

        public async UniTask PlayerSync(ReceivedPacket receivedPacket)
        {
            //테스트용 플레이어 등록
            if (FindPlayer(receivedPacket.Token) == null)
            {
                await RegisterPlayer(receivedPacket);
            }
            // 페이로드 분기
            // 싱크 패킷이 플레이어 무브일 때
            if (receivedPacket.Payload is C2SPlayerMove playerMove)
            {
                Protocol.Position direction = playerMove.Position;
                await PlayerSyncNotify((uint)receivedPacket.ClientId, (byte)PlayerActionType.MOVE, direction.ToVector3(), 1000, receivedPacket.Token);
            }
            // 싱크 패킷이 플레이어 액션일 때
            else if (receivedPacket.Payload is S2CPlayerActionRes playerAction)
            {
                //액션타입이 플레이어액션에서는 uint이고 플레이어싱크에서는 바이트임 수정할 필요 있어보임
                uint playerActionType = playerAction.ActionType;
                Vector3 direction = FindPlayer(receivedPacket.Token).position;
                uint playerEquippedItem = (uint)PlayerEquippedItem.NONE;
                if (playerAction.Success)
                {
                    playerEquippedItem = playerAction.TargetObjectId;
                }
                //일단 오류 꼴 뵈기 싫어서 바이트로 형변환은 하지만 무조건 수정해야할거같음
                await PlayerSyncNotify((uint)receivedPacket.ClientId, (byte)playerActionType, direction, playerEquippedItem, receivedPacket.Token);
            }
            else if (receivedPacket.Payload is S2CMoveToAreaMapRes moveArea)
            {
                var player = FindPlayer(receivedPacket.Token);
                var playerEquippedItem = player.EquippedItem;
                if(moveArea.JoinMapResultCode == 1)
                {
                    // 맵의 위치를 받아서 플레이어의 위치를 수정해 줌
                    PlayerMoveArea(moveArea.DestinationAreaType, receivedPacket.Token);
                }
                await PlayerSyncNotify((uint)receivedPacket.ClientId, 0, Vector3.zero, (uint)playerEquippedItem, receivedPacket.Token);
            }
            else
            {
                //아마 여기다 missing 뭐 이런거 보내주면 될 듯
            }
            
            return;
        }

        private UniTask PlayerSyncNotify(uint ClientId, byte ActionType, Vector3 direction, uint EquippedItem, string token)
        {
            Logger.Log($"기존 움직임 방향은 : {direction}");
            Logger.Log($"direction : {direction}, ActionType : {ActionType}, token : {token}, direction.magnitude : {direction.magnitude}");
            //if (direction.magnitude < 100) // dash가 얼마나 될 지 모르니 일단 100
            //{
            //    Correction(direction, ActionType, token);                
            //}
            //else // 이동 거리가 너무 클 경우 움직이지 않게 함
            //{
            //    Correction(Vector3.zero, ActionType, token);
            //}
            Correction(direction, ActionType, token);
            List<Protocol.PlayerState> playerStates = new();
            if (TryGetTeam((int)ClientId, out Team allPlayers))
            {
                foreach (var player in allPlayers.Players.Values) // 플레이어 목록 순회
                {
                    playerStates.Add(new Protocol.PlayerState
                    {
                        PlayerId = (uint)player.ClientId,
                        ActionType = (uint)player.ActionType,
                        Position = player.position.FromVector(),
                        EquippedItem = (uint)player.EquippedItem,
                    });
                }
            }

            var packet = new Packet
            {
                PacketType = PacketType.SYNC_PLAYERS_NOTIFICATION,

                //Token = "", // 특정 클라이언트에게 전송 시 설정
                Payload = new Protocol.S2CSyncPlayersNoti
                {
                    PlayerStates = { playerStates },
                }
            };
            Logger.Log("플레이어 싱크 패킷" + packet);
            SendPacketForAll(packet);
            return UniTask.CompletedTask;
        }

        public Protocol.Position Correction(Vector3 direction, byte ActionType, string token)
        {
            // 속도 검사하는 로직이 빠져있고 이거는 대쉬 하면서 같이 만들 예정
            // 이동 가능한 위치인지도 빠져있다.
            // 밑에 함수는 포지션만 전달에서 플레이어 안에서 처리를 한다 
            FindPlayer(token).ChangePosition(direction);
            FindPlayer(token).ChangeAction(ActionType);
            return FindPlayer(token).position.FromVector();

        }
        
        public void PlayerMoveArea(uint DestinationAreaType, string token)
        {
            FindPlayer(token).ChangeArea(DestinationAreaType);
        }

        /// <summary>
        /// 클라이언트 제거 처리.
        /// 팀 목록에서도 제거
        /// </summary>
        /// <param name="clientId">제거할 클라이언트 ID</param>
        public async UniTask ClientRemove(int clientId)
        {
            if (GameState == State.LOBBY)
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

        public async UniTask GameStart(ReceivedPacket receivedPacket)
        {

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
                    return UniTask.FromException(new System.InvalidOperationException($"Unknown client : {player.ClientId}"));
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
