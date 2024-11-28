using StackExchange.Redis;
using System.Linq;

namespace ResourceWar.Server
{
    public class Player
    {
        public readonly int ClientId;

        public string UserName { get; set; }
        public bool IsReady { get; set; }
        public bool Connected { get; set; }
        public int LoadProgress { get; set; }
        public int TeamId { get; set; }
        public int AvatarId { get; set; }

        public Player(int clientId)
        {
            ClientId = clientId;
            IsReady = false;
            Connected = true;
            LoadProgress = 0;
            TeamId = 0;
        }

        public static Player FromRedisData(int clientId, HashEntry[] redisValues)
        {
            return new Player(clientId)
            {
                UserName = (redisValues.First(x => x.Name == "user_name").Value).ToString(),
                IsReady = bool.Parse(redisValues.First(x => x.Name == "is_ready").Value),
                Connected = bool.Parse(redisValues.First(x => x.Name == "connected").Value),
                LoadProgress = int.Parse(redisValues.First(x => x.Name == "load_progress").Value),
                TeamId = int.Parse(redisValues.First(x => x.Name == "team_id").Value),
                AvatarId = int.Parse(redisValues.First(x => x.Name == "avatar_id").Value)
            };
        }

        public HashEntry[] ToRedisHashEntries()
        {
            return new HashEntry[]
            {
                new HashEntry("user_name", UserName.ToString()),
                new HashEntry("is_ready", IsReady.ToString()),
                new HashEntry("connected", Connected.ToString()),
                new HashEntry("load_progress", LoadProgress.ToString()),
                new HashEntry("team_id", TeamId.ToString()),
                new HashEntry("avatar_id", AvatarId.ToString())
            };
        }
    }
}
