using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public class Furnace
    {
        public SyncFurnaceStateCode State { get; private set; } = SyncFurnaceStateCode.WAITING;
        public float Progress { get; private set; } = 0.0f; // 진행도 (0~100%)

        private CancellationTokenSource progressToken;

        public void StartProgress()
        {
            if (progressToken != null)
            {
                progressToken.Cancel();
            }

            progressToken= new CancellationTokenSource();
            IntervalManager.Instance.AddTask(
                $"Furnace_{GetHashCode()}",
                async token =>
                {
                    Progress += 10;
                    if (Progress >= 100 && Progress < 150)
                    {
                        State = SyncFurnaceStateCode.PRODUCING;
                    }
                    else if (Progress >= 150)
                    {
                        State = SyncFurnaceStateCode.OVERFLOW;
                    }
                    await UniTask.CompletedTask;
                },
                1.0f);
        }

        public void ResetProgress()
        {
            Progress = 0;
            State = SyncFurnaceStateCode.WAITING;
            IntervalManager.Instance.CancelTask(progressToken.Token);
        }

        public void UpdateState(SyncFurnaceStateCode newState)
        {
            State = newState;
        }

        public void UpdateProgress(float newProgress)
        {
            Progress = Math.Clamp(newProgress, 0.0f, 150.0f);
            UpdateStateBasedOnProgress();
        }

        private void UpdateStateBasedOnProgress()
        {
            if (Progress < 1.0f)
                State = SyncFurnaceStateCode.WAITING;
            else if (Progress < 100.0f)
                State = SyncFurnaceStateCode.RUNNING;
            else if (Progress < 150.0f)
                State = SyncFurnaceStateCode.PRODUCING;
            else
                State = SyncFurnaceStateCode.OVERFLOW;
        }
    }
}
