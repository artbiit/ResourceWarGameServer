using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Player 
    {
        public readonly int ClientId;
        public string playerName;
        public Vector3 position = Vector3.zero;
        public int avartarItem;
        public long playerLatency = 0;
        public long lastSendTime = 0;
        public int playerSpeed = 100;
        

        public Player(int clientId)
        {
            this.ClientId = clientId;
            this.playerName = clientId.ToString();
            this.avartarItem = 1;
        }
    }
}
