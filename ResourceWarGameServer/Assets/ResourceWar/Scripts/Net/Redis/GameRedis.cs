using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public static class GameRedis
    {
        private static readonly string GAME_SESSION_KEY = "GameSession";
        private static readonly string ALIVE_GAMESSESIONS_KEY = "GameSessions";
        private static readonly string LOBBY_QUEUE_KEY = "Lobby";
        
        public static async UniTask<bool> SetGameState(ServerState state)
        {
           return await RedisClient.Instance.ExecuteAsync(db => db.HashSetAsync(GAME_SESSION_KEY, "state", (int)state));
        }

        public static async UniTask<long> RemoveFromLobby(string gameCode)
        {
            return await RedisClient.Instance.ExecuteAsync(db => db.ListRemoveAsync(LOBBY_QUEUE_KEY, gameCode));
        }




        
    }
}
