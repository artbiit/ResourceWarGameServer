using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Player 
    {
        public readonly int ClientId;

        public Player(int clientId)
        {
            this.ClientId = clientId;
        }
    }
}
