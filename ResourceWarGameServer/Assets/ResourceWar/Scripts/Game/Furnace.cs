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
        private Action<SyncFurnaceStateCode, int, float, string> onStateUpdate;

        public void SetAction(Action<SyncFurnaceStateCode, int, float, string> stateUpdateCallback)
        {
            onStateUpdate = stateUpdateCallback;
        }

        public Action<SyncFurnaceStateCode, int, float, string> GetAction() => onStateUpdate;

        public FurnaceResultCode FurnaceStateProcess(Player player, int teamIndex, string clientToken)
        {
            FurnaceResultCode resultCode = FurnaceResultCode.SUCCESS;

            // 용광로 상태 처리
            switch (State)
            {
                case SyncFurnaceStateCode.WAITING:
                    if (player.EquippedItem == (int)PlayerEquippedItem.IRONSTONE)
                    {
                        player.EquippedItem = (int)PlayerEquippedItem.NONE;
                        StartProgress(teamIndex, clientToken);
                    }
                    else
                    {
                        resultCode = FurnaceResultCode.INVALID_ITEM;
                    }
                    break;
                case SyncFurnaceStateCode.PRODUCING:
                case SyncFurnaceStateCode.OVERFLOW:
                    {
                        player.EquippedItem = State == SyncFurnaceStateCode.PRODUCING
                            ? (int)PlayerEquippedItem.IRON
                            : (int)PlayerEquippedItem.GARBAGE;
                        ResetProgress();
                        StartProgress(teamIndex, clientToken);
                    }
                    break;
                case SyncFurnaceStateCode.RUNNING:
                    {
                        resultCode = FurnaceResultCode.RUNNING_STATE;
                    }
                    break;
                default:
                    resultCode = FurnaceResultCode.FAIL;
                    break;
            }

            return resultCode;
        }

        public void StartProgress(int teamIndex, string clientToken)
        {
            StopProgress();

            progressToken= new CancellationTokenSource();
            IntervalManager.Instance.AddTask(
                $"Furnace_{GetHashCode()}",
                async token =>
                {
                    Progress += 10;
                    UpdateStateBasedOnProgress();

                    // 상태 업데이트 콜백 실행
                    onStateUpdate?.Invoke(State, teamIndex, Progress, clientToken);

                    await UniTask.CompletedTask;
                },
                1.0f);
        }

        public void StopProgress()
        {
            if (progressToken != null)
            {
                IntervalManager.Instance.CancelTask(progressToken.Token);
                progressToken = null;
            }
        }

        public void ResetProgress()
        {
            StopProgress();
            Progress = 0;
            State = SyncFurnaceStateCode.WAITING;
        }

        public void Reset()
        {
            StopProgress();
            Progress = 0;
            State = SyncFurnaceStateCode.WAITING;
            onStateUpdate = null;
        }

        private void UpdateStateBasedOnProgress()
        {
            if (Progress <= 0.001f)
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
