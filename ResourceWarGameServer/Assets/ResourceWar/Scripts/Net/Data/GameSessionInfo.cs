
namespace ResourceWar.Server
{
    public class GameSessionInfo
    {
        /// <summary>
        /// 비공개 방인지
        /// </summary>
        public bool isPrivate { get; set; }

        /// <summary>
        /// 방장의 게임 토큰
        /// </summary>
        public string roomMaster { get; set; }

        /// <summary>
        /// 게임 세션 상태
        /// </summary>
        public GameSessionState state { get; set; }
        /// <summary>
        /// 해당 게임 세션으로 접속하는 URL
        /// </summary>
        public string gameUrl { get; set; } = string.Empty;
        /// <summary>
        /// 최대 접속 가능한 플레이어
        /// </summary>
        public int maxPlayer { get; set; } = 4;
        /// <summary>
        /// 현 로비에 접속된 플레이어 수
        /// </summary>
        public int currentPlayer { get; set; } = 0;
        /// <summary>
        /// 연결은 했으나 인증되지 않은 플레이어 수
        /// </summary>
        public int previousPlayer { get; set; } = 0;
        /// <summary>
        /// 세션 생성 시점
        /// </summary>
        public long createdAt { get; set; } = UnixTime.Now();
        /// <summary>
        /// 마지막으로 정보가 갱신된 시점
        /// </summary>
        public long updatedAt { get; set; } = UnixTime.Now();

    }

}