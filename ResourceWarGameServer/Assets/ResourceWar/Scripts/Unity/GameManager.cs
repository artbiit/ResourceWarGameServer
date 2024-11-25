using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;

namespace ResourceWar.Server
{
    public class GameManager : MonoBehaviour
    {
        // 게임 상태를 나타내는 열거형(enum)
        public enum State
        {
            CREATING,  // 게임이 생성 중인 상태
            DESTROY,   // 게임이 종료되거나 파괴된 상태
            LOBBY,     // 로비 화면
            LOADING,   // 게임이 로드 중인 상태
            PLAYING    // 게임이 진행 중인 상태
        }

        // 현재 게임 상태를 나타내는 프로퍼티 (초기 상태는 CREATING)
        public State GameState { get; private set; } = State.CREATING;

        // 게임에 접근하는 인증 토큰
        public string GameToken { get; private set; }

        // 플레이어 정보를 저장하는 Dictionary, 키는 토큰, 값은 Player 객체
        private Dictionary<string, Player> players = new Dictionary<string, Player>();

        /// <summary>
        /// 초기화 함수
        /// 게임 상태를 CREATING으로 설정하고, 플레이어 데이터를 초기화함
        /// </summary>
        public async UniTaskVoid Init()
        {
            GameState = State.CREATING;  // 초기 상태 설정
            players.Clear();             // 플레이어 목록 초기화
        }
    }
}
