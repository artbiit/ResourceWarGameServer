using ResourceWar.Server;
using System.Collections.Generic;
using System;
using Google.Protobuf;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace ResourceWar.Server
{
    public static class PacketUtils
    {
        // PacketType과 메시지명을 매핑
        private static readonly Dictionary<PacketType, string> PacketTypeToMessageName = new()
{
    // 게임 상태 관련 데이터
    { PacketType.GAME_STATE, "Protocol.GameState" },
    { PacketType.ITEM_DATA, "Protocol.ItemData" },
    { PacketType.PLAYER_ROOM_INFO, "Protocol.PlayerRoomInfo" },
    { PacketType.PLAYER_INITIAL_DATA, "Protocol.PlayerInitialData" },
    { PacketType.PLAYER_STATE, "Protocol.PlayerState" },
    { PacketType.FIELD_UNIT, "Protocol.FieldUnit" },
    { PacketType.POSITION, "Protocol.Position" },

    // 회원가입
    { PacketType.SIGN_UP_REQUEST, "Protocol.C2SSignUpReq" },
    { PacketType.SIGN_UP_RESPONSE, "Protocol.S2CSignUpRes" },

    // 로그인
    { PacketType.SIGN_IN_REQUEST, "Protocol.C2SSignInReq" },
    { PacketType.SIGN_IN_RESPONSE, "Protocol.S2CSignInRes" },

    // 로그아웃
    { PacketType.SIGN_OUT_REQUEST, "Protocol.C2SSignOutReq" },
    { PacketType.SIGN_OUT_RESPONSE, "Protocol.S2CSignOutRes" },

    // 토큰 재발급
    { PacketType.REFRESH_TOKEN_REQUEST, "Protocol.C2SRefreshTokenReq" },
    { PacketType.REFRESH_TOKEN_RESPONSE, "Protocol.S2CRefreshTokenRes" },

    // 대기실 관련 요청 및 응답
    { PacketType.CREATE_ROOM_REQUEST, "Protocol.C2SCreateRoomReq" },
    { PacketType.CREATE_ROOM_RESPONSE, "Protocol.S2CCreateRoomRes" },
    { PacketType.MATCH_START_REQUEST, "Protocol.C2SMatchStartReq" },
    { PacketType.MATCH_START_RESPONSE, "Protocol.S2CMatchStartRes" },
    { PacketType.MATCH_CANCEL_REQUEST, "Protocol.C2SMatchCancelReq" },
    { PacketType.MATCH_CANCEL_RESPONSE, "Protocol.S2CMatchCancelRes" },
    { PacketType.MATCH_PROGRESS_NOTIFICATION, "Protocol.S2CMatchProgressNoti" },

    // 대기실 입장 및 나가기
    { PacketType.JOIN_ROOM_REQUEST, "Protocol.C2SJoinRoomReq" },
    { PacketType.JOIN_ROOM_RESPONSE, "Protocol.S2CJoinRoomRes" },
    { PacketType.QUIT_ROOM_REQUEST, "Protocol.C2SQuitRoomReq" },
    { PacketType.QUIT_ROOM_NOTIFICATION, "Protocol.S2CQuitRoomNoti" },

    // 팀 변경
    { PacketType.TEAM_CHANGE_REQUEST, "Protocol.C2STeamChangeReq" },
    { PacketType.TEAM_CHANGE_RESPONSE, "Protocol.S2CTeamChangeRes" },
    { PacketType.TEAM_CHANGE_NOTIFICATION, "Protocol.S2CTeamChangeNoti" },

            // 로비 팀 준비
            {PacketType.PLAYER_IS_READY_CHANGE_REQUEST, "Protocol.C2SPlayerIsReadyChangeReq" },
            {PacketType.PLAYER_IS_READY_CHANGE_RESPONSE, "Protocol.S2CPlayerIsReadyChangeRes" },

    // 대기실 동기화 및 게임 시작
    { PacketType.SYNC_ROOM_NOTIFICATION, "Protocol.S2CSyncRoomNoti" },
    { PacketType.GAME_START_REQUEST, "Protocol.C2SGameStartReq" },
    { PacketType.GAME_START_NOTI, "Protocol.S2CGameStartRes" },

    // 로딩 진행
    { PacketType.LOAD_PROGRESS_NOTIFICATION, "Protocol.S2CLoadProgressNoti" },
    { PacketType.SYNC_LOAD_NOTIFICATION, "Protocol.S2CSyncLoadNoti" },

    // 초기 정보 동기화
    { PacketType.INITIAL_NOTIFICATION, "Protocol.S2CInitialNoti" },

    // 플레이어 및 상태 동기화
    { PacketType.SYNC_PLAYERS_NOTIFICATION, "Protocol.S2CSyncPlayersNoti" },
    { PacketType.SYNC_FURNACE_STATE_NOTIFICATION, "Protocol.S2CSyncFurnaceStateNoti" },
    { PacketType.SAWMILL_STATUS_NOTIFICATION, "Protocol.S2CSawmillStatusNoti" },
    { PacketType.WORKBENCH_STATUS_NOTIFICATION, "Protocol.S2CWorkbenchStatusNoti" },

    // 플레이어 액션
    { PacketType.PLAYER_ACTION_REQUEST, "Protocol.C2SPlayerActionReq" },
    { PacketType.PLAYER_ACTION_RESPONSE, "Protocol.S2CPlayerActionRes" },
    { PacketType.PLAYER_MOVE, "Protocol.C2SPlayerMove" },

    // 객체 관련
    { PacketType.SPAWN_OBJECT_NOTIFICATION, "Protocol.S2CSpawnObjectNoti" },
    { PacketType.DESTROY_OBJECT_REQUEST, "Protocol.C2SDestroyObjectReq" },
    { PacketType.DESTROY_OBJECT_NOTIFICATION, "Protocol.S2CDestroyObjectNoti" },

    // 전장 유닛 동기화
    { PacketType.SYNC_FIELD_UNIT_NOTIFICATION, "Protocol.S2CSyncFieldUnitNoti" },

    // 항복
    { PacketType.SURRENDER_REQUEST, "Protocol.C2SSurrenderReq" },
    { PacketType.SURRENDER_RESPONSE, "Protocol.S2CSurrenderRes" },
    { PacketType.SURRENDER_NOTIFICATION, "Protocol.S2CSurrenderNoti" },

    // 게임 종료
    { PacketType.GAME_OVER_NOTIFICATION, "Protocol.S2CGameOverNoti" },

    // 공용맵 이동
    { PacketType.MOVE_TO_AREA_MAP_REQUEST, "Protocol.C2SMoveToAreaMapReq" },
    { PacketType.MOVE_TO_AREA_MAP_RESPONSE, "Protocol.S2CMoveToAreaMapRes" },

    // 조합대
    { PacketType.WORKBENCH_REQUEST, "Protocol.C2SWorkbenchReq" },
    { PacketType.WORKBENCH_RESPONSE, "Protocol.S2CWorkbenchRes" },

    // 용광로
    { PacketType.FURNACE_REQUEST, "Protocol.C2SFurnaceReq" },
    { PacketType.FURNACE_RESPONSE, "Protocol.S2CFurnaceRes" },

    // 제재소
    { PacketType.SAWMILL_REQUEST, "Protocol.C2SSawmillReq" },
    { PacketType.SAWMILL_RESPONSE, "Protocol.S2CSawmillRes" },

    // 헬스체크
    { PacketType.PING_REQUEST, "Protocol.S2CPingReq" },
    { PacketType.PONG_RESPONSE, "Protocol.C2SPongRes" },

    // 대기실 매칭 요청
    { PacketType.MATCH_REQUEST, "Protocol.C2SMatchReq" },
    { PacketType.MATCH_RESPONSE, "Protocol.S2CMatchRes" },
    //게임 서버 연결 후 인증
    { PacketType.AUTHORIZE_REQUEST, "Protocol.C2SAuthorizeReq" },
    { PacketType.AUTHORIZE_RESPONSE, "Protocol.S2CAuthorizeRes" },
            //몬스터 출격 관련
            {PacketType.MONSTER_DEPLOY_REQUEST, "Protocol.C2SMonsterDeployReq" },
            {PacketType.MONSTER_DEPLOY_NOTI, "Protocol.S2CMonsterDeployNoti" },

    // 에러
    { PacketType.MISSING_FIELD, "Protocol.S2CMissingFieldNoti" },
    { PacketType.NEED_AUTHORIZE, "Protocol.S2CNeedAuthorizeNoti" },
            //테스트용 코드
#if UNITY_EDITOR
            { PacketType.ADD_MONSTER_REQUEST, "Protocol.C2SMonsterAddReq" }

#endif

        };

        private static ConcurrentDictionary<string, Type> CachedMessageTypes = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 메시지명으로 메시지 타입을 매핑하여 IMessage 객체를 동적으로 생성합니다.
        /// </summary>
        public static IMessage CreateMessage(string messageName)
        {
            // 메시지명을 통해 Type 찾기
            var messageType = GetMessageType(messageName);

            // IMessage로 캐스팅 가능한 객체 생성
            if (Activator.CreateInstance(messageType) is not IMessage message)
            {
                throw new InvalidOperationException($"The type '{messageName}' does not implement IMessage.");
            }

            return message;
        }

        /// <summary>
        /// PacketType으로 메시지명을 가져옵니다.
        /// </summary>
        public static string GetMessageName(PacketType packetType)
        {
            if (PacketTypeToMessageName.TryGetValue(packetType, out var messageName))
            {
                return messageName;
            }

            throw new ArgumentException($"Invalid PacketType: {packetType}");
        }

        /// <summary>
        /// PacketType으로 IMessage 객체를 생성합니다.
        /// </summary>
        public static IMessage CreateMessage(PacketType packetType)
        {
            string messageName = GetMessageName(packetType);
            return CreateMessage(messageName);
        }

        // 빅 엔디안으로 UInt16 읽기
        public static ushort ReadUInt16BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes); // 리틀 엔디안 -> 빅 엔디안 변환
            }
            return BitConverter.ToUInt16(bytes, 0);
        }

        // 빅 엔디안으로 Int32 읽기
        public static int ReadInt32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes); // 리틀 엔디안 -> 빅 엔디안 변환
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        public static Type GetMessageType(PacketType packetType) => GetMessageType(GetMessageName(packetType));

        public static Type GetMessageType(string messageName)
        {
            Type messageType = null;
            if (CachedMessageTypes.TryGetValue(messageName, out var cachedType))
            {
                messageType = cachedType;
            }
            else
            {
                messageType = Type.GetType(messageName);
                if (messageType == null)
                {
                    throw new ArgumentException($"Invalid message name: {messageName}");
                }
                CachedMessageTypes.TryAdd(messageName, messageType);
            }


            return messageType;
        }

        /// <summary>
        /// 해당 메세지가 실제 타입과 같은지 검사합니다.
        /// </summary>
        /// <param name="message">CreateMessage()로 생성된 메세지</param>
        /// <param name="messageType">실제 클래스 타입</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsSameMessageType(IMessage message, PacketType packetType)
        {
            if (message == null)
            {
                throw new ArgumentNullException("Message cannot be null.");
            }

            // 메시지의 실제 타입과 비교
            return message.GetType() == GetMessageType(packetType);
        }
    }
}