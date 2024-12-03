using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> FurnaceHandler(ReceivedPacket packet)
        {
            var result = new Packet
            {
                PacketType = PacketType.FURNACE_RESPONSE
            };

            var resultCode = FurnaceResultCode.SUCCESS;
            C2SFurnaceReq furnaceMessage = (C2SFurnaceReq)packet.Payload;
            var itemCode = furnaceMessage.Item.ItemCode;

            // 아이템이 Ironstone인지 유효 검사
            if (!TableData.Items.TryGetValue((int)itemCode, out ItemTableData item) && item.ItemType != ItemTypes.Ironstone)
            {
                Logger.LogError($"FurnaceHandler: Item is not a Ironstone. itemCode: {itemCode}");
                resultCode = FurnaceResultCode.INVALID_ITEM;
            }

            if (string.IsNullOrWhiteSpace(packet.Token))
            {
                Logger.LogError("FurnaceHandler: Token is null or empty.");
                resultCode = FurnaceResultCode.FAIL;
            }

            if (resultCode == FurnaceResultCode.SUCCESS)
            {

            }

            result.Token = "";
            result.Payload = new S2CFurnaceRes
            {
                FurnaceResultCode = (uint)resultCode,
            };

            return result;
        }
    }
}
