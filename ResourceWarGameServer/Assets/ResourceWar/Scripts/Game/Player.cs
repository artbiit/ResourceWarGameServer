using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Player 
    {
        public readonly int ClientId;
        public string playerName;
        public Vector3 position;
        public int avartarItem;

        public Player(int clientId)
        {
            this.ClientId = clientId;
            this.playerName = clientId.ToString();
            this.position = PositionExtensions.ToVector3();
            this.avartarItem = 1;
        }
    }
}
