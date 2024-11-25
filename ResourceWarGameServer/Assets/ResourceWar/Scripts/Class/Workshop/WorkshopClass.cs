using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ResourceWar.Server
{
    public enum WorkShopState
    {
        Idle,
        Ready,
        InProgress,
        Completed
    }
    public abstract class WorkshopClass
    {
        private readonly int id;
        private readonly int gameTeamId; // 어떤 팀의 것인지
        private readonly int itemId; // 사용 가능 재료 (고정)
        private int itemAmount; // 필요할 것 같진 않음

        protected float progress;
        protected WorkShopState state;

        protected WorkshopClass(int id, int gameTeamId, int itemId, int itemAmount = 0)
        {
            this.id = id;
            this.gameTeamId = gameTeamId;
            this.itemId = itemId;
            this.itemAmount = itemAmount;
            this.state = WorkShopState.Idle;
            this.progress = 0;
        }

        // 재료 넣기
        public void AddItem()
        {
            if (this.state != WorkShopState.Idle) return;

            this.state = WorkShopState.Ready;
        }

        // 제작 진행
        public virtual void StartProcessing()
        {
            if (state != WorkShopState.Ready) return;

            this.state = WorkShopState.InProgress;
            // 로직 처리
        }

        // 진행 업데이트
        public virtual void UpdateProgress(float deltaTime)
        {
            if (state != WorkShopState.InProgress) return;

            this.progress += deltaTime * 10; // 초당 10%진행
            if (this.progress >= 100)
            {
                this.progress = 100;
                this.state = WorkShopState.Completed;
            }
        }

        // 상태 초기화
        public void ResetWorkshop()
        {
            this.progress = 0;
            this.state = WorkShopState.Idle;
        }

        public WorkShopState GetState() => this.state;

        public float GetProgress()
        {
            return this.progress;
        }
    }
}
