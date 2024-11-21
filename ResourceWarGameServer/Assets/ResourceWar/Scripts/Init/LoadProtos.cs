using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using Protocol;
using UnityEditor.VersionControl;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class ProtoLoader
    {
        /// <summary>
        /// Protocol.cs의 모든 Protobuf 메시지를 ProtoMessageRegistry에 등록
        /// </summary>
        public static void LoadProtos()
        {
            // 메시지 이름과 패킷 타입 매핑
            var packetTypeMappings = new Dictionary<string, ushort>
            {
                { "protocol.GameState", 1 },
                { "protocol.ItemData", 2 },
                { "protocol.PlayerRoomInfo", 3 },
                { "protocol.PlayerInitialData", 4 },
                { "protocol.PlayerState", 5 },
                { "protocol.FieldUnit", 6 },
                { "protocol.Position", 7 },
                
                // 회원가입 및 로그인
                { "protocol.C2SSignUpReq", 8 },
                { "protocol.S2CSignUpRes", 9 },
                { "protocol.C2SSignInReq", 10 },
                { "protocol.S2CSignInRes", 11 },
                
                // 토큰 재발급
                { "protocol.C2SRefreshTokenReq", 12 },
                { "protocol.S2CRefreshTokenRes", 13 },
                
                // 대기실 관련 요청 및 응답
                { "protocol.C2SCreateRoomReq", 14 },
                { "protocol.S2CCreateRoomRes", 15 },
                { "protocol.C2SMatchCancelReq", 16 },
                { "protocol.S2CMatchProgressNoti", 17 },
                
                // 대기실 입장 및 나가기
                { "protocol.C2SJoinRoomReq", 18 },
                { "protocol.S2CJoinRoomRes", 19 },
                { "protocol.C2SQuitRoomReq", 20 },
                { "protocol.S2CQuitRoomNoti", 21 },
                
                // 팀 변경
                { "protocol.C2STeamChangeReq", 22 },
                { "protocol.S2CTeamChangeNoti", 23 },
                
                // 대기실 동기화 및 게임 시작
                { "protocol.S2CSyncRoomNoti", 24 },
                { "protocol.C2SGameStartReq", 25 },
                { "protocol.S2CGameStartRes", 26 },
                
                // 로딩 진행
                { "protocol.C2SLoadProgressNoti", 27 },
                { "protocol.S2CSyncLoadNoti", 28 },
                
                // 초기 정보 동기화
                { "protocol.S2CInitialNoti", 29 },
                
                // 플레이어 및 상태 동기화
                { "protocol.S2CSyncPlayersNoti", 30 },
                { "protocol.S2CSyncFurnaceStateNoti", 31 },
                { "protocol.S2CSawmillStatusNoti", 32 },
                { "protocol.S2CWorkbenchStatusNoti", 33 },
                
                // 플레이어 액션
                { "protocol.C2SPlayerActionReq", 34 },
                { "protocol.S2CPlayerActionRes", 35 },
                
                // 객체 관련
                { "protocol.S2CSpawnObjectNoti", 36 },
                { "protocol.C2SDestoryObjectReq", 37 },
                { "protocol.S2CDestoryObjectNoti", 38 },
                
                // 전장 유닛 동기화
                { "protocol.S2CSyncFieldUnitNoti", 39 },
                
                // 항복
                { "protocol.C2SSurrenderReq", 40 },
                { "protocol.S2CSurrenderRes", 41 },
                { "protocol.S2CSurrenderNoti", 42 },
                
                // 게임 종료
                { "protocol.S2CGameOverNoti", 43 },
                
                // 공용맵 이동
                { "protocol.C2SMoveToAreaMapReq", 44 },
                { "protocol.S2CMoveToAreaMap", 45 },
                
                // 조합대
                { "protocol.C2SWorkbenchReq", 46 },
                { "protocol.S2CWorkbenchRes", 47 },
                
                // 용광로
                { "protocol.C2SFurnaceReq", 48 },
                { "protocol.S2CFurnaceRes", 49 },
                
                // 제재소
                { "protocol.C2SSawmillReq", 50 },
                { "protocol.S2CSawmillRes", 51 },
                
                // 헬스체크
                { "protocol.S2CPingReq", 52 },
                { "protocol.C2SPongRes", 53 },
                
                // 대기실 매칭 요청
                { "protocol.C2SMatchReq", 54 },
                { "protocol.S2CMatchRes", 55 },
                
                // 에러 처리
                { "protocol.MissingFieldError", 10000 }
            };

            // ProtocolReflection.Descriptor의 모든 메시지를 순회
            foreach (var messageDescriptor in ProtocolReflection.Descriptor.MessageTypes)
            {
                // 메시지 인스턴스 생성 
                var messageInstance = messageDescriptor.Parser.ParseFrom(new byte[0]);
                Logger.Log($"Initialized IMessage instance: {messageInstance}");


                //메시지 이름으로 패킷 타입 매핑
                if (packetTypeMappings.TryGetValue(messageDescriptor.FullName, out var packetType))
                {
                    // ProtoMessageRegistry에 메시지 등록
                    ProtoMessageRegistry.RegisterMessage(packetType, messageInstance);
                    Logger.Log($"Registered Protobuf message: {messageDescriptor.FullName} with PacketType: {packetType}");
                    // Logger.Log($"Registered Protobuf message: {messageDescriptor.FullName} with Fields : {string.Join(',', messageDescriptor.Fields.InDeclarationOrder().Select(s => s.Name))}");
                } 
                else
                {
                    // 매핑되지 않은 메시지 경고`
                    Logger.LogWarning($"No packet type mapping found for: {messageDescriptor.FullName}");
                }
            }
        }
        /// <summary>
        /// 필드 타입에 따라 기본값을 반환
        /// </summary>
        private static object GetDefaultValueForField(FieldDescriptor field)
        {
            return field.FieldType switch
            {
                FieldType.Int32 => 0,
                FieldType.Int64 => 0L,
                FieldType.UInt32 => 0u,
                FieldType.UInt64 => 0ul,
                FieldType.Bool => false,
                FieldType.String => string.Empty,
                FieldType.Message => field.MessageType?.Parser?.ParseFrom(new byte[0]),
                FieldType.Float => 0f,
                FieldType.Double => 0.0,
                FieldType.Enum => field.EnumType.Values.First().Number,
                _ => null
            };
        }
    }
}
