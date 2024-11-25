using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;


namespace ResourceWar.Server
{
    public class GameManager : MonoBehaviour
    {
        public enum State
        {
            CREATING,
            DESTROY,
            LOBBY,
            LOADING,
            PLAYING
        }

        public State GameState { get; private set; } = State.CREATING;
        public string GameToken { get; private set; }
      
        /// <summary>
        /// Token - Player
        /// </summary>
        private Dictionary<string, Player> players = new Dictionary<string, Player>();
        
        public async UniTaskVoid Init()
        {
            GameState = State.CREATING;
            players.Clear();   
        }


        

        

    }
}
