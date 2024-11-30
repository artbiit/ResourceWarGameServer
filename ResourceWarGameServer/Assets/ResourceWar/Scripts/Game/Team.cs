using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Team
    {
        public readonly Dictionary<string, Player> Players = new();

        public bool ContainsPlayer(string token) => Players.ContainsKey(token);

        /// <summary>
        /// 팀 정보 초기 상태로 돌려놓는 용도
        /// </summary>
        public void Reset()
        {
            foreach (var player in Players)
            {
                player.Value.Disconnected();
            }
            Players.Clear();
        }
    }
}
