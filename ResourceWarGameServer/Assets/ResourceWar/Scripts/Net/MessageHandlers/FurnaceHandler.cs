using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System.Collections.Generic;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        // FurnaceClass 인스턴스를 관리하는 Dictionary
        private readonly Dictionary<int, FurnaceClass> furnaces = new();

        /// <summary>
        /// 클라이언트가 용광로(Furnace)에 재료를 추가하는 요청을 처리합니다.
        /// </summary>
        /// <param name="packet">클라이언트로부터 수신된 패킷</param>
        /// <returns>응답 패킷</returns>
        public async UniTask<Packet> HandleAddItemToFurnace(Packet packet)
        {
            var request = (C2SFurnaceReq)packet.Payload;

            if (furnaces.TryGetValue((int)request.Item.ItemCode, out var furnace))
            {
                furnace.AddItem();
                Logger.Log($"Furnace {request.Item.ItemCode} received AddItem request.");

                var response = new Packet
                {
                    PacketType = PacketType.FURNACE_RESPONSE,
                    Payload = new S2CFurnaceRes
                    {
                        FurnaceResultCode = 0 // 성공
                    }
                };
                return response;
            }
            else
            {
                Logger.LogError($"Furnace {request.Item.ItemCode} not found.");

                return new Packet
                {
                    PacketType = PacketType.FURNACE_RESPONSE,
                    Payload = new S2CFurnaceRes
                    {
                        FurnaceResultCode = 1 // 실패
                    }
                };
            }
        }

        /// <summary>
        /// 클라이언트가 용광로 제작을 시작하는 요청을 처리합니다.
        /// </summary>
        /// <param name="packet">클라이언트로부터 수신된 패킷</param>
        /// <returns>응답 패킷</returns>
        public async UniTask<Packet> HandleStartFurnaceProcessing(Packet packet)
        {
            var request = (C2SFurnaceReq)packet.Payload;

            if (furnaces.TryGetValue((int)request.Item.ItemCode, out var furnace))
            {
                furnace.StartProcessing();
                Logger.Log($"Furnace {request.Item.ItemCode} started processing.");

                return new Packet
                {
                    PacketType = PacketType.FURNACE_RESPONSE,
                    Payload = new S2CFurnaceRes
                    {
                        FurnaceResultCode = 0 // 성공
                    }
                };
            }
            else
            {
                Logger.LogError($"Furnace {request.Item.ItemCode} not found.");

                return new Packet
                {
                    PacketType = PacketType.FURNACE_RESPONSE,
                    Payload = new S2CFurnaceRes
                    {
                        FurnaceResultCode = 1 // 실패
                    }
                };
            }
        }

        /// <summary>
        /// 용광로 상태를 클라이언트에 알리는 요청을 처리합니다.
        /// </summary>
        /// <param name="packet">클라이언트로부터 수신된 패킷</param>
        /// <returns>응답 패킷</returns>
        public async UniTask<Packet> HandleFurnaceStateRequest(Packet packet)
        {
            var request = (C2SFurnaceReq)packet.Payload;

            if (furnaces.TryGetValue((int)request.Item.ItemCode, out var furnace))
            {
                var state = furnace.GetState();
                var progress = furnace.GetProgress();

                Logger.Log($"Furnace {request.Item.ItemCode} state: {state}, progress: {progress}");

                return new Packet
                {
                    PacketType = PacketType.SYNC_FURNACE_STATE_NOTIFICATION,
                    Payload = new S2CSyncFurnaceStateNoti
                    {
                        TeamIndex = (uint)furnace.GameTeamId(),
                        FurnaceStateCode = (uint)state,
                        Progress = progress
                    }
                };
            }
            else
            {
                Logger.LogError($"Furnace {request.Item.ItemCode} not found.");

                return new Packet
                {
                    PacketType = PacketType.FURNACE_RESPONSE,
                    Payload = new S2CFurnaceRes
                    {
                        FurnaceResultCode = 1 // 실패
                    }
                };
            }
        }

        /// <summary>
        /// 새로운 용광로를 등록하거나 초기화합니다.
        /// </summary>
        /// <param name="furnaceId">용광로 ID</param>
        /// <param name="gameTeamId">게임 팀 ID</param>
        /// <param name="itemId">아이템 ID</param>
        /// <param name="itemAmount">아이템 양</param>
        public void RegisterFurnace(int furnaceId, int gameTeamId, int itemId, int itemAmount)
        {
            if (furnaces.ContainsKey(furnaceId))
            {
                Logger.Log($"Furnace {furnaceId} already exists.");
                return;
            }

            var furnace = new FurnaceClass(furnaceId, gameTeamId, itemId, itemAmount);
            furnaces[furnaceId] = furnace;

            Logger.Log($"Furnace {furnaceId} registered for team {gameTeamId}.");
        }
    }
}
