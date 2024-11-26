using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public class FurnaceClass : WorkshopClass
    {
        private readonly int updateInterval = 1000; // 진행 업데이트 간격 (밀리초)
        private bool isRunning = false; // 제작 중 여부를 추적하는 플래그

        public FurnaceClass(int id, int gameTeamId, int itemId, int itemAmount = 0) 
            : base(id, gameTeamId, itemId, itemAmount)
        {
        }

        /// <summary>
        /// 재료를 용광로에 추가합니다.
        /// 상태를 Ready로 변경하고 클라이언트에 알림을 보냅니다.
        /// </summary>
        public override void AddItem()
        {
            base.AddItem();
            NotifyClient("재료가 추가되었습니다.", this.state, GetProgress());
        }

        /// <summary>
        /// 용광로의 제작을 시작합니다.
        /// 상태를 InProgress로 변경하고, 제작 진행을 비동기적으로 처리합니다.
        /// </summary>
        public override void StartProcessing()
        {
            // 이미 실행 중이거나 Ready 상태가 아니면 중단
            if (isRunning || this.state != WorkShopState.Ready) return; 

            base.StartProcessing();
            NotifyClient("제작이 시작되었습니다.", this.state, GetProgress());

            isRunning = true;
            // 비동기로 진행 상태 업데이트
            ProcessProgress().Forget();
        }

        /// <summary>
        /// 제작 진행 상황을 업데이트합니다.
        /// 진행률이 100%에 도달하면 상태를 Completed로 변경하고 클라이언트에 알림을 보냅니다.
        /// </summary>
        public override void UpdateProgress(float deltaTime)
        {
            base.UpdateProgress(deltaTime);
            NotifyClient("진행 중입니다.", WorkShopState.InProgress, GetProgress());

            if (GetState() == WorkShopState.Completed)
            {
                NotifyClient("아이템이 완성되었습니다!", WorkShopState.Completed, GetProgress());
                ResetWorkshop();  // 워크샵 상태 초기화
                isRunning = false;
            }
        }

        /// <summary>
        /// 용광로의 진행 상태를 업데이트하는 비동기 메서드입니다.
        /// 진행률을 주기적으로 업데이트하고, 상태 변화에 따라 알림을 전송합니다.
        /// </summary>
        private async UniTaskVoid ProcessProgress()
        {
            while (GetState() == WorkShopState.InProgress)
            {
                UpdateProgress(1); // deltaTime = 1초
                // 업데이트 간격 대기
                await UniTask.Delay(updateInterval);
            }
        }

        /// <summary>
        /// 클라이언트에 용광로 상태와 메시지를 전송합니다.
        /// </summary>
        /// <param name="message">전송할 메시지</param>
        /// <param name="state">현재 워크샵 상태</param>
        /// <param name="progress">진행률</param>
        private void NotifyClient(string message, WorkShopState state, float progress)
        {
            var packet = new Packet
            {
                PacketType = PacketType.SYNC_FURNACE_STATE_NOTIFICATION, // 패킷 타입
                Token = "", // 특정 클라이언트에게 전송 시 설정
                Payload = new Protocol.S2CSyncFurnaceStateNoti
                {
                    TeamIndex = (uint)this.gameTeamId, // 팀 ID
                    FurnaceStateCode = (uint)state, // 현재 상태
                    Progress = progress // 진행률
                }
            };

            // EventDispatcher를 사용해 클라이언트에 패킷 전송
            EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance
                .NotifyAsync(GameManager.GameManagerEvent.SendPacketForTeam, packet)
                .Forget();
        }
    }
}
