using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    /// <summary>
    /// 레디스에 보관하고 있는 유저 세션 정보
    /// </summary>
    public struct UserSession
    {
        /// <summary>
        /// 만료 Unix시간
        /// </summary>
        public long ExpirationTime;
        /// <summary>
        /// DB User ID
        /// </summary>
        public int ID;

      public UserSession(Dictionary<RedisValue, RedisValue> data) {
            ID = int.Parse(data["id"]);
            ExpirationTime = long.Parse(data["expirationTime"]);
      }
    }
}
