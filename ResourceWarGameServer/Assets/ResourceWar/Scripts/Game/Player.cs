using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Player 
    {
        public readonly int ClientId;
        public int Team {  get; private set; }

        public Player(int clientId)
        {
            this.ClientId = clientId;
        }

        public void ChangeTeam(int team) => this.Team = team;
    }
}
