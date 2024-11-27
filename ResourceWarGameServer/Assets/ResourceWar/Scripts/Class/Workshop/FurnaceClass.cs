using Cysharp.Threading.Tasks;
using Protocol;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    /// <summary>
    /// 용광로(Furnace)의 동작 및 상태 관리를 담당하는 클래스
    /// </summary>
    public class FurnaceClass : WorkshopClass
    {
        public FurnaceClass(int teamId) : base(teamId)
        {
        }

        public WorkShopResultCode AddItem(int itemId)
        {
            // 아이템 검증 로직 추가 가능
            Logger.Log($"Item {itemId} added to furnace for TeamId: {TeamId}");
            StartProcessing(); // 작업 시작
            // 나중에 플레이어 상호작용키 들어오면 실행하는 부분생각
            return WorkShopResultCode.SUCCESS;
        }

        public async UniTask NotifyProgressAsync()
        {
            while (IsProcessing)
            {
                Progress += 10; // 진행률
                Logger.Log($"Furnace Progress for TeamId {TeamId}: {Progress}%");

                var progressPacket = new S2CSyncFurnaceStateNoti
                {
                    TeamIndex = (uint)TeamId,
                    Progress = Progress,
                    FurnaceStateCode = (uint)(Progress >= 100 ? WorkShopResultCode.SUCCESS : WorkShopResultCode.PROGRESS)
                };

                await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(
                    GameManager.GameManagerEvent.SendPacketForTeam,
                    new Packet
                    {
                        PacketType = PacketType.SYNC_FURNACE_STATE_NOTIFICATION,
                        Payload = progressPacket
                    });

                if (Progress >= 100)
                {
                    StopProcessing();
                    break;
                }

                await UniTask.Delay(1000); // 1초 간격으로 진행률 업데이트
            }
        }
    }
}
