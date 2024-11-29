using Cysharp.Threading.Tasks;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class PlayerRedis
    {
        private const string PLAYER_LIST_KEY_FORMAT = "Game:{0}:Players";
        private const string PLAYER_KEY_FORMAT = "Game:{0}:Player:{1}";

        public static async UniTask AddPlayerInfo(
           string gameToken,
           int clientId,
           string userName,
           bool isReady,
           bool connected,
           int loadProgress,
           int teamId,
           int avatarId,
           int ttlSeconds = 7200)
        {
            // 플레이어 정보를 저장할 Redis 키
            string playerKey = string.Format(PLAYER_KEY_FORMAT, gameToken, clientId);

            // Redis Hash 구조로 저장할 데이터
            var fields = new HashEntry[]
            {
                new HashEntry("user_name", userName.ToString()),
                new HashEntry("is_ready", isReady.ToString()),
                new HashEntry("connected", connected.ToString()),
                new HashEntry("load_progress", loadProgress.ToString()),
                new HashEntry("team_id", teamId.ToString()),
                new HashEntry("avatar_id", avatarId.ToString()),
            };

            // Redis에 플레이어 정보 저장
            await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync(playerKey, fields));
            await RedisClient.Instance.ExecuteAsync(db => db.KeyExpireAsync(playerKey, TimeSpan.FromSeconds(ttlSeconds)));

            // 게임별 플레이어 리스트에 플레이어 추가
            string playerListKey = string.Format(PLAYER_LIST_KEY_FORMAT, gameToken);
            await RedisClient.Instance.ExecuteAsync(db => db.ListRightPushAsync(playerListKey, clientId.ToString()));
        }

    }
}
