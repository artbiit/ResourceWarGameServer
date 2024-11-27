using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ResourceWar.Server
{
    public abstract class WorkshopClass
    {
        protected int TeamId { get; private set; }
        protected float Progress { get; set; }
        protected bool IsProcessing { get; set; }

        public WorkshopClass(int teamId)
        {
            TeamId = teamId;
            Progress = 0;
            IsProcessing = false;
        }

        /// <summary>
        /// 작업을 시작합니다.
        /// </summary>
        public virtual void StartProcessing()
        {
            if (IsProcessing) return;
            IsProcessing = true;
        }

        /// <summary>
        /// 현재 진행률을 반환합니다.
        /// </summary>
        public virtual float GetProgress()
        {
            return Progress;
        }

        /// <summary>
        /// 작업을 중단합니다.
        /// </summary>
        public virtual void StopProcessing()
        {
            IsProcessing = false;
        }
    }
}
