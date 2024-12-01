using Cysharp.Threading.Tasks;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class GameRedis
    {
    
        public enum KEY
        {
            GameSession, //해당 게임 세션에 대한 정보
            GameSessions, //모든 게임 세션에 대한 열거를 위한 리스트
            Lobby, //매치메이킹으로 입장이 가능한 서버 목록
            WaitingGames, //방 배정없이 대기 중인 게임 서버 목록
            NewWaitingGameServer, //새로 배정된 게임 서버가 있음을 알리는 채널
        }

        /// <summary>
        /// 게임 정보 중 상태 정보만 변경
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static async UniTask<bool> SetGameState(string gameCode, GameSessionState state)
        {
           return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync($"{KEY.GameSession}:{gameCode}", "state", (int)state));
        }

        /// <summary>
        /// 게임 정보 중 방장만 변경
        /// </summary>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public static async UniTask<bool> SetRoomMaster(string gameCode, string userToken)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync($"{KEY.GameSession}:{gameCode}", "roomMaster", userToken));
        }

        /// <summary>
        /// 방 비공개 여부 변경
        /// </summary>
        /// <param name="isPrivate"></param>
        /// <returns></returns>
        public static async UniTask<bool> SetLobbyPrivate(string gameCode, bool isPrivate)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync($"{KEY.GameSession}:{gameCode}", "isPrivate", isPrivate));
        }

        /// <summary>
        /// 해당 방에 접속 정보 수정
        /// </summary>
        /// <param name="gameCode"></param>
        /// <param name="gameUrl"></param>
        /// <returns></returns>
        public static async UniTask<bool> SetGameUrl(string gameCode, string gameUrl)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync($"{KEY.GameSession}:{gameCode}", "gameUrl", gameUrl));
        }

        /// <summary>
        /// 해당 방에 접속및 인증된 유저 수 수정
        /// </summary>
        /// <param name="gameCode"></param>
        /// <param name="currentPlayer"></param>
        /// <returns></returns>
        public static async UniTask<bool> SetCurrentPlayerCount(string gameCode, int currentPlayer)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync($"{KEY.GameSession}:{gameCode}", "currentPlayer", currentPlayer));
        }

        /// <summary>
        /// 해당 방에 접속했으나 인증 대기 중인 유저 수 수정
        /// </summary>
        /// <param name="gameCode"></param>
        /// <param name="priviousPlayer"></param>
        /// <returns></returns>
        public static async UniTask<bool> SetPreviousPlayerCount(string gameCode, int priviousPlayer)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync($"{KEY.GameSession}:{gameCode}", "priviousPlayer", priviousPlayer));
        }
     


        /// <summary>
        /// 로비 목록에서 게임 코드 제거하기
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        public static async UniTask<long> RemoveFromLobby(string gameCode)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.ListRemoveAsync(KEY.Lobby.ToString(), gameCode));
        }

        /// <summary>
        /// 전체 게임세션 정보 한번에 가져오기 (매번 인스턴스 생성하니 남발 하지 말것)
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        public static async UniTask<GameSessionInfo> GetGameSessionInfo(string gameCode)
        {
            return await RedisClient.Instance.LoadObjectFromHash<GameSessionInfo>($"{KEY.GameSession}:{gameCode}");
        }

        /// <summary>
        /// Hset - 게임 세션 정보 전체 한번에 등록
        /// </summary>
        /// <param name="gameCode"></param>
        /// <param name="gameSessionInfo"></param>
        /// <returns></returns>
        public static async UniTask SetGameSessionInfo(string gameCode, GameSessionInfo gameSessionInfo)
        {
             await RedisClient.Instance.SaveObjectToHash<GameSessionInfo>($"{KEY.GameSession}:{gameCode}", gameSessionInfo, 7200);
        }

        /// <summary>
        /// 대기중인 게임 서버 목록에 추가합니다.
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        public static async UniTask<long> AddWaitingGameServer(string gameCode)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.ListRightPushAsync(KEY.WaitingGames.ToString(), gameCode));
        }

        /// <summary>
        /// 대기중인 게임 서버 목록에서 제거합니다.
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        public static async UniTask<long> RemoveWaitingGameServer(string gameCode)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.ListRemoveAsync(KEY.WaitingGames.ToString(), gameCode));
        }

        /// <summary>
        /// 게임 세션에 대한 모든 정보를 소거합니다. 로비 목록에서도 제거합니다.
        /// </summary>
        /// <param name="gameCode"></param>
        /// <param name="gameSessionInfo"></param>
        /// <returns></returns>
        public static async UniTask RemoveGameSessionInfo(string gameCode)
        {
         await UniTask.WhenAll(
             RedisClient.Instance.ExecuteAsync(db => db.KeyDeleteAsync($"{KEY.GameSession}:{gameCode}")),
             RemoveFromLobby(gameCode),
             RemoveWaitingGameServer(gameCode)
             );
        }

        /// <summary>
        /// 게임 서버에 대한 모든 정보를 등록합니다. 로비 목록엔 추가하지 않습니다.
        /// </summary>
        /// <param name="gameCode"></param>
        /// <param name="sessionInfo"></param>
        /// <returns></returns>
        public static async UniTask AddGameSessionInfo(string gameCode, GameSessionInfo sessionInfo)
        {
            await UniTask.WhenAll(SetGameSessionInfo(gameCode, sessionInfo), 
                AddWaitingGameServer(gameCode),
                PublishNewWaitingGameServer(gameCode));
            
        }

        /// <summary>
        /// 새 게임 서버가 준비되었음을 알립니다.
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
       public static async UniTask<long> PublishNewWaitingGameServer(string gameCode)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.PublishAsync(KEY.NewWaitingGameServer.ToString(), gameCode));
        }

    }
}
