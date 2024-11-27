using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using UnityEditor.VersionControl;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        /*// 아이템 데이터
        message ItemData
        {
            uint32 itemCode = 1;
            uint32 itemType = 2;
            uint32 amount = 3;
        }*/
        /// <summary>
        /// 클라이언트가 용광로에 재료를 추가하는 요청을 처리합니다.
        /// 패킷이 올바르게 왔는지 검증합니다.
        /// </summary>
        public async UniTask<Packet> HandleAddItemToFurnace(ReceivedPacket packet)
        {
            string token = packet.Token;
            // 패킷이 null인지 확인
            if (packet == null || packet.Payload == null)
            {
                return CreateErrorResponse(PacketType.FURNACE_RESPONSE, token, "Invalid packet.");
            }

            // 패킷 타입 검증
            if (packet.Payload is not C2SFurnaceReq request)
            {
                return CreateErrorResponse(PacketType.FURNACE_RESPONSE, token, "Invalid payload type.");
            }

            // 요청 데이터 검증 (예: ItemId 확인)
            if (request.Item.ItemCode <= 0)
            {
                return CreateErrorResponse(PacketType.FURNACE_RESPONSE, token, "Invalid ItemId.");
            }

            Logger.Log($"Packet validation succeeded for ItemId: {request.Item.ItemCode}");

            // 패킷이 유효하다면 성공 응답 생성
            return new Packet
            {
                PacketType = PacketType.FURNACE_RESPONSE,
                Token = token,
                Payload = new S2CFurnaceRes { FurnaceResultCode = (uint)WorkShopResultCode.SUCCESS }
            };
        }

        /// <summary>
        /// 오류 응답을 생성합니다.
        /// </summary>
        private Packet CreateErrorResponse(PacketType packetType, string token = "", string errorMessage = "아무튼 실패")
        {
            Logger.LogError(errorMessage);
            return new Packet
            {
                PacketType = packetType,
                Token = token,
                Payload = new S2CFurnaceRes
                {   
                    FurnaceResultCode = (uint)WorkShopResultCode.FAIL,
                }
            };
        }
    }
}
