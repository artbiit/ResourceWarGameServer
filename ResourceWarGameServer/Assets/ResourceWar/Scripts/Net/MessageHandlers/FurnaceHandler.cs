using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;
using Protocol;
using Cysharp.Threading.Tasks;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private FurnaceClass furnace;

        public void FurnaceHandler()
        {
            // Furnace 초기화는 게임 시작 시 호출되는 메서드에서 수행
            InitializeFurnace();
        }

        // Furnace 초기화 메서드
        public void InitializeFurnace()
        {
            furnace = new FurnaceClass(1, 0, 1001); // id: 1, teamId: 0, itemId: 1001
            Logger.Log("Furnace initialized.");
        }

        private async UniTask<Packet> FurnaceHandler(Packet packet)
        {
            var request = (C2SFurnaceReq)packet.Payload;
            Logger.Log($"Received Furnace Request with ItemId: {request.Item}");

            // 상태에 따라 처리
            if (furnace.GetState() == WorkShopState.Idle)
            {
                // 재료 추가
                furnace.AddItem();
                Logger.Log("Furnace state changed to Ready.");
            }
            else if (furnace.GetState() == WorkShopState.Ready)
            {
                // 제작 시작
                furnace.StartProcessing();
                Logger.Log("Furnace state changed to InProgress.");
            }
            else if (furnace.GetState() == WorkShopState.InProgress)
            {
                // 진행 업데이트
                furnace.UpdateProgress(0.1f); // deltaTime: 0.1초
                Logger.Log($"Furnace progress: {furnace.GetProgress()}%");

                if (furnace.GetState() == WorkShopState.Completed)
                {
                    Logger.Log("Furnace state changed to Completed.");
                }
            }

            // 클라이언트로 상태 동기화 메시지 전송
            Packet response = new Packet();
            S2CSyncFurnaceStateNoti payload = new S2CSyncFurnaceStateNoti
            {
                TeamIndex = 0, // 팀 ID (임시 값)
                FurnaceStateCode = (uint)furnace.GetState(),
                Progress = furnace.GetProgress()
            };
            response.Payload = payload;
            response.PacketType = PacketType.FURNACE_RESPONSE;
            
            Logger.Log($"Sending Furnace State: {payload.FurnaceStateCode}, Progress: {payload.Progress}%");

            return response;
        }
    }
}
