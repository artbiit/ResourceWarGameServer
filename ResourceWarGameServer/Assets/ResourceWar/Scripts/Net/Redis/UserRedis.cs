using Cysharp.Threading.Tasks;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class UserRedis
    {
        private static readonly string USER_SESSION_KEY = "UserSession";
        public static async UniTask<Dictionary<RedisValue, RedisValue>> GetUserSession(string token)
        {
            var userSession = await RedisClient.Instance.ExecuteAsync(db => db.HashGetAllAsync($"{USER_SESSION_KEY}:{token}"));
            if(userSession?.Length <= 0)
            {
                return null;
            }

            return userSession.ToDictionary();
        }

#if UNITY_EDITOR
       static int masterCount = 0;
#endif
        /// <summary>
        /// 특정 토큰을 사용하여 nickName을 가져옵니다.
        /// </summary>
        /// <param name="token">세션 토큰</param>
        /// <returns>nickName</returns>
        public static async UniTask<string> GetNickName(string token)
        {
#if UNITY_EDITOR
            if(token.StartsWith( "master"))
            {
                return $"m{masterCount++}aster{new string('@', masterCount)}";
            }

#endif
            // Redis에서 User 데이터에서 nickName 필드 가져오기
            var nickName = await RedisClient.Instance.ExecuteAsync(db => db.HashGetAsync($"{USER_SESSION_KEY}:{token}", "nickname"));
            return nickName.IsNullOrEmpty ? null : nickName.ToString();
        }

        /// <summary>
        /// 특정 토큰을 사용하여 nickName을 가져옵니다.
        /// </summary>
        /// <param name="token">세션 토큰</param>
        /// <returns>nickName</returns>
        public static async UniTask<int> GetPlayerId(string token)
        {
            // Redis에서 User 데이터에서 nickName 필드 가져오기
            var id = await RedisClient.Instance.ExecuteAsync(db => db.HashGetAsync($"{USER_SESSION_KEY}:{token}", "id"));
            if (id.TryParse(out int intValue))
            {
                Logger.Log($"변환 성공: {intValue}");
            }
            else
            {
                Logger.Log("변환 실패");
                intValue = 0;
            }
            return intValue;
        }
    }
}
