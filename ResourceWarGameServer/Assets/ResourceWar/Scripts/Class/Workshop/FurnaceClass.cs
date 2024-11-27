using Cysharp.Threading.Tasks;

namespace ResourceWar.Server
{
    /// <summary>
    /// 용광로(Furnace)의 동작 및 상태 관리를 담당하는 클래스
    /// </summary>
    public class FurnaceClass : WorkshopClass
    {
        public int UpdateInterval { get; } = 1000; // 진행 상태 업데이트 간격 (밀리초)
        public string CurrentToken { get; private set; } // 현재 작업 중인 클라이언트의 Token

        public FurnaceClass(int id, int gameTeamId, int itemId, int itemAmount = 0)
            : base(id, gameTeamId, itemId, itemAmount) { }

        /// <summary>
        /// 용광로에서 제작을 시작합니다.
        /// </summary>
        public override void StartProcessing()
        {
            if (GetState() != WorkShopState.Ready) return; // 상태가 Ready가 아니면 진행 중단

            base.StartProcessing();
            NotifyClient("Processing started.", WorkShopState.InProgress, GetProgress());
        }

        /// <summary>
        /// 클라이언트에 현재 용광로 상태를 알립니다.
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        /// <param name="state">현재 상태</param>
        /// <param name="progress">진행률</param>
        public void NotifyClient(string message, WorkShopState state, float progress)
        {
            var packet = new Packet
            {
                PacketType = PacketType.SYNC_FURNACE_STATE_NOTIFICATION,
                Token = this.CurrentToken,
                Payload = new Protocol.S2CSyncFurnaceStateNoti
                {
                    TeamIndex = (uint)this.gameTeamId,
                    FurnaceStateCode = (uint)state,
                    Progress = progress
                }
            };

            // 클라이언트에 패킷 전송
        }

        /// <summary>
        /// 작업 시작 시 Token을 설정합니다.
        /// </summary>
        /// <param name="token">클라이언트의 Token</param>
        public void SetToken(string token)
        {
            this.CurrentToken = token;
        }
    }
}
