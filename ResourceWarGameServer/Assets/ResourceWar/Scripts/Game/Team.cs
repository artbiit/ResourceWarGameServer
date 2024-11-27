using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Team
    {
        public readonly int TeamId;
        public readonly Dictionary<string, Player> Players = new();
        public FurnaceClass Furnace { get; private set; } // 팀별 용광로
        

        public bool ContainsPlayer(string token) => Players.ContainsKey(token);
        
        public Team(int teamId)
        {
            TeamId = teamId;
            Furnace = new FurnaceClass(teamId); // 팀 생성 시 용광로 생성
        }

        public Player GetPlayer(string token)
        {
            Players.TryGetValue(token, out var player);
            return player;
        }
    }
}
