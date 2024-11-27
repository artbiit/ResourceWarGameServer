using Cysharp.Threading.Tasks;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }
}
