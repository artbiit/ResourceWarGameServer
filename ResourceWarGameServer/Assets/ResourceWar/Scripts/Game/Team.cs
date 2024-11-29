using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Team
    {
        public readonly Dictionary<string, Player> Players = new();

        public bool ContainsPlayer(string token) => Players.ContainsKey(token);

        public bool HasPlayers()
        {
            return Players.Count > 0;
        }
    }
}
