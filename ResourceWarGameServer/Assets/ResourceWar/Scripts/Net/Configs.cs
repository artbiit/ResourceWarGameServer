using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public enum PacketLength : int
    {
        PACKET_TYPE_LENGTH = 2, // 패킷타입을 나타내는 길이
        PACKET_TOKEN_LENGTH = 1, // 버전 문자열 길이
        PACKET_PAYLOAD_LENGTH = 4, // 페이로드 데이터 길이
    }

    public enum PacketType : ushort
    {
        // 게임 상태 관련 데이터
        GAME_STATE = 1,
        ITEM_DATA = 2,
        PLAYER_ROOM_INFO = 3,
        PLAYER_INITIAL_DATA = 4,
        PLAYER_STATE = 5,
        FIELD_UNIT = 6,
        POSITION = 7,

        // 회원가입
        SIGN_UP_REQUEST = 8,
        SIGN_UP_RESPONSE = 9,

        // 로그인
        SIGN_IN_REQUEST = 10,
        SIGN_IN_RESPONSE = 11,

        // 로그아웃
        SIGN_OUT_REQUEST = 56,
        SIGN_OUT_RESPONSE = 57,

        // 토큰 재발급
        REFRESH_TOKEN_REQUEST = 12,
        REFRESH_TOKEN_RESPONSE = 13,

        // 대기실 관련 요청 및 응답
        CREATE_ROOM_REQUEST = 14,
        CREATE_ROOM_RESPONSE = 15,
        MATCH_START_REQUEST = 58, // 매칭 신청
        MATCH_START_RESPONSE = 59, // 매칭 시작 여부 알림
        MATCH_CANCEL_REQUEST = 16,
        MATCH_CANCEL_RESPONSE = 60,
        MATCH_PROGRESS_NOTIFICATION = 17, // 매칭 진행 여부 알림

        // 대기실 입장 및 나가기
        JOIN_ROOM_REQUEST = 18,
        JOIN_ROOM_RESPONSE = 19,
        QUIT_ROOM_REQUEST = 20,
        QUIT_ROOM_NOTIFICATION = 21,

        // 팀 변경
        TEAM_CHANGE_REQUEST = 22,
        TEAM_CHANGE_NOTIFICATION = 23,

        // 대기실 동기화 및 게임 시작
        SYNC_ROOM_NOTIFICATION = 24,
        GAME_START_REQUEST = 25,
        GAME_START_RESPONSE = 26,

        // 로딩 진행
        LOAD_PROGRESS_NOTIFICATION = 27,
        SYNC_LOAD_NOTIFICATION = 28,

        // 초기 정보 동기화
        INITIAL_NOTIFICATION = 29,

        // 플레이어 및 상태 동기화
        SYNC_PLAYERS_NOTIFICATION = 30,
        SYNC_FURNACE_STATE_NOTIFICATION = 31,
        SAWMILL_STATUS_NOTIFICATION = 32,
        WORKBENCH_STATUS_NOTIFICATION = 33,

        // 플레이어 액션
        PLAYER_ACTION_REQUEST = 34,
        PLAYER_ACTION_RESPONSE = 35,
        PLAYER_MOVE = 61,

        // 객체 관련
        SPAWN_OBJECT_NOTIFICATION = 36,
        DESTROY_OBJECT_REQUEST = 37,
        DESTROY_OBJECT_NOTIFICATION = 38,

        // 전장 유닛 동기화
        SYNC_FIELD_UNIT_NOTIFICATION = 39,

        // 항복
        SURRENDER_REQUEST = 40,
        SURRENDER_RESPONSE = 41,
        SURRENDER_NOTIFICATION = 42,

        // 게임 종료
        GAME_OVER_NOTIFICATION = 43,

        // 공용맵 이동
        MOVE_TO_AREA_MAP_REQUEST = 44,
        MOVE_TO_AREA_MAP_RESPONSE = 45,

        // 조합대
        WORKBENCH_REQUEST = 46,
        WORKBENCH_RESPONSE = 47,

        // 용광로
        FURNACE_REQUEST = 48,
        FURNACE_RESPONSE = 49,

        // 제재소
        SAWMILL_REQUEST = 50,
        SAWMILL_RESPONSE = 51,

        // 헬스체크
        PING_REQUEST = 52,
        PONG_RESPONSE = 53,

        // 대기실 매칭 요청
        MATCH_REQUEST = 54,
        MATCH_RESPONSE = 55,

        //게임 서버 유저 인증을 위한 요청
        AUTHORIZE_REQUEST = 62,
        AUTHORIZE_RESPONSE = 63,

        // 에러
        MISSING_FIELD = 10000, // 요청 파라미터 재점검 필요
        NEED_AUTHORIZE = 10001, //인증 후 다른 패킷 하도록 요청
    }
}
