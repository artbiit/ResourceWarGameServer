using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public class Team
    {
        // key - userToken
        public readonly Dictionary<string, Player> Players = new();

        public Furnace TeamFurnace { get; private set; } = new Furnace();

        public bool ContainsPlayer(string token) => Players.ContainsKey(token);

        public void LoopTeamPlayers(System.Func<string, Player, bool> func)
        {
            foreach (var playerPair in Players)
            {
                if(!func.Invoke(playerPair.Key, playerPair.Value))
                {
                    break;
                }
            }
        }

        public bool TryGetPlayer(int clientId, out Player player)
        {
            Player foundPlayer = null;
            LoopTeamPlayers((token, teamPlayer) => {
                if (teamPlayer.ClientId == clientId)
                {
                    foundPlayer = teamPlayer;
                    return false;
                }
                return true;
            });

            player = foundPlayer;
            return player != null;
        }

        public bool TryGetUserToken(int clientId, out string token)
        {
            string UserToken = string.Empty;
            LoopTeamPlayers((token, teamPlayer) => {
                if (teamPlayer.ClientId == clientId)
                {
                    UserToken = token;
                    return false;
                }
                return true;
            });
            token = UserToken;
            return string.IsNullOrWhiteSpace(token) == false;
        }

        public bool TryRemoveByClient(int clientId)
        {
            string UserToken = string.Empty;
            LoopTeamPlayers((token, teamPlayer) => {
                if (teamPlayer.ClientId == clientId)
                {
                    UserToken = token;
                    return false;
                }
                return true;
            });

            if (string.IsNullOrWhiteSpace(UserToken) == false)
            {
                this.Players[UserToken].Disconnected();
                return this.Players.Remove(UserToken);
            }
            return false;
        }


        public bool HasPlayers()
        {
            return Players.Count > 0;
        }

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
            TeamFurnace = new Furnace();
        }
    }
}
