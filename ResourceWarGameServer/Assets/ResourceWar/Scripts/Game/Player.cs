using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Player 
    {
        public readonly int ClientId;
        public readonly int TeamId;

        public Player(int clientId, int teamId)
        {
            this.ClientId = clientId;
            this.TeamId = teamId;
        }

        public int GetTeamId() => this.TeamId;
    }
}
