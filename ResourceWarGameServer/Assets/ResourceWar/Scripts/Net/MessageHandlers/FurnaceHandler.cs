using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        /// <summary>
        /// 클라이언트가 용광로에 재료를 추가하는 요청을 처리합니다.
        /// </summary>
        public async UniTask<Packet> HandleAddItemToFurnace(ReceivedPacket packet)
        {
            var request = (C2SFurnaceReq)packet.Payload; // 요청 데이터를 Protobuf 메시지로 변환
            var player = GameManager.FindPlayer(packet.Token); // 플레이어 찾기

            if (player == null)
            {
                Logger.LogError("Player not found for token: " + packet.Token);
                return null; // 플레이어를 찾지 못하면 null 반환
            }

            var furnace = gameManager.GetFurnaceByTeamId(player.TeamId); // 팀 ID 기반 용광로 가져오기
            if (furnace == null)
            {
                Logger.LogError($"Furnace not found for TeamId: {player.TeamId}");
                return null; // 용광로가 없으면 null 반환
            }

            furnace.AddItem(); // 재료 추가 로직 호출
            Logger.Log($"Item added to furnace for TeamId: {player.TeamId}");

            // 성공 응답 패킷 생성
            return new Packet
            {
                PacketType = PacketType.FURNACE_RESPONSE,
                Token = packet.Token,
                Payload = new S2CFurnaceRes { FurnaceResultCode = 0 }
            };
        }

        /// <summary>
        /// 용광로 진행 상태를 일정 주기로 클라이언트에 알립니다.
        /// </summary>
        /// <param name="furnace">진행 상태를 알릴 대상 용광로</param>
        public void StartNotifyingFurnaceState(FurnaceClass furnace)
        {
            furnace.StartProcessing(); // 용광로 진행 시작
            NotifyFurnaceStateAsync(furnace).Forget(); // 비동기 상태 알림
        }

        private async UniTaskVoid NotifyFurnaceStateAsync(FurnaceClass furnace)
        {
            while (furnace.GetState() == WorkShopState.InProgress) // 상태가 진행 중인 경우
            {
                var progressPacket = new Packet
                {
                    PacketType = PacketType.SYNC_FURNACE_STATE_NOTIFICATION,
                    Token = furnace.CurrentToken, // 클라이언트에서 받은 Token 값 사용
                    Payload = new S2CSyncFurnaceStateNoti
                    {
                        TeamIndex = (uint)furnace.GameTeamId(),
                        FurnaceStateCode = (uint)furnace.GetState(),
                        Progress = furnace.GetProgress()
                    }
                };

                await gameManager.SendPacketForTeam(progressPacket);
                await UniTask.Delay(furnace.UpdateInterval); // 지정된 간격만큼 대기
            }
        }
    }
}
