using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ResourceWar.Server.GameManager;

namespace ResourceWar.Server
{
    public class Furnace
    {
        public enum Event
        {
            FurnaceRequest
        }
        public SyncFurnaceStateCode State { get; private set; } = SyncFurnaceStateCode.WAITING;
        public float Progress { get; private set; } = 0.0f; // 진행도 (0~100%)

        private CancellationTokenSource progressToken;

        public Furnace()
        {
            EventDispatcher<Event,ReceivedPacket>
                .Instance
                .Subscribe(Event.FurnaceRequest, FurnaceResponseHandler);
        }

        public async UniTask FurnaceResponseHandler(ReceivedPacket receivedPacket)
        {
            var clientId = receivedPacket.ClientId;
            var token = receivedPacket.Token;

            var player = await DataDispatcher<int, Player>
                .Instance.RequestDataAsync(clientId);
            var (teamIndex, team) = await DataDispatcher<int, (int teamIndex, Team team)>
                .Instance.RequestDataAsync(clientId);
            FurnaceResultCode resultCode = this.FurnaceStateProcess(player, teamIndex, token);

            var packet = new Packet
            {
                PacketType = PacketType.FURNACE_RESPONSE,
                Token = token,
                Payload = new S2CFurnaceRes
                {
                    FurnaceResultCode = (uint)resultCode,
                }
            };
            await EventDispatcher<GameManager.GameManagerEvent, Packet>
                .Instance.NotifyAsync(GameManagerEvent.SendPacketForUser, packet);
        }

        public FurnaceResultCode FurnaceStateProcess(Player player, int teamIndex, string clientToken)
        {
            FurnaceResultCode resultCode = FurnaceResultCode.SUCCESS;

            // 용광로 상태 처리
            switch (State)
            {
                case SyncFurnaceStateCode.WAITING:
                    if (player.EquippedItem == PlayerEquippedItem.IRONSTONE)
                    {
                        player.EquippedItem =  PlayerEquippedItem.NONE;
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
                            ?  PlayerEquippedItem.IRON
                            :  PlayerEquippedItem.GARBAGE;
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
                    OnFurnaceStateUpdate(State, teamIndex, Progress, clientToken);
                    await UniTask.CompletedTask;
                },
                1.0f);
        }

        private async void OnFurnaceStateUpdate(SyncFurnaceStateCode state, int teamIndex, float progress, string token)
        {
            if (state == SyncFurnaceStateCode.WAITING)
            {
                return; // WAITING 상태는 동기화 하지 않음
            }

            var syncPacket = new Packet
            {
                PacketType = PacketType.SYNC_FURNACE_STATE_NOTIFICATION,
                Token = token,
                Payload = new S2CSyncFurnaceStateNoti
                {
                    TeamIndex = (uint)teamIndex,
                    FurnaceStateCode = (uint)state,
                    Progress = progress
                }
            };

           await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForTeam, syncPacket);
        }


        public void StopProgress()
        {
            if (progressToken != null)
            {
                IntervalManager.Instance.CancelTask(progressToken.Token);
                progressToken.Dispose();
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
