using Cysharp.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Protocol;
using ResourceWar.Server.Lib;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class Player
    {
        public  int ClientId;

        public string UserName { get; set; }
        public bool IsReady { get; set; }
        public bool IsConnected { get; set; }
        public int LoadProgress { get; set; }
        public int TeamId { get; set; }
        public int AvatarId { get; set; }

        public long Latency { get; private set; }
        public long RoundTripTime { get; private set; }

        private Queue<long> pingQueue = new();
        private string hashCode;
        CancellationToken pingToken;

        public Player(int clientId)
        {
            IsReady = false;
            IsConnected = true;
            LoadProgress = 0;
            TeamId = 0;
            this.hashCode = this.GetHashCode().ToString();
            Connected(clientId);
           
        }

        public void Connected(int clientId)
        {
            Logger.Log($"Player is reconnected {this.ClientId} -> {clientId}");
            this.ClientId = clientId;
            this.IsConnected = true;
            EventDispatcher<(int, int), long>.Instance.Subscribe((this.ClientId, int.MaxValue + this.ClientId), PongRes);
            pingToken = IntervalManager.Instance.AddTask(hashCode, PingReq, 1.0f);
        }

        public void Disconnected()
        {
            Logger.Log($"Player[{this.ClientId} is disconnected");
            IntervalManager.Instance.CancelTask(pingToken);
            EventDispatcher<(int, int), long>.Instance.Unsubcribe((this.ClientId, int.MaxValue + this.ClientId), PongRes);
            this.IsConnected = false;
        }


        private async UniTask PingReq(CancellationToken token)
        {
            if(token.IsCancellationRequested) return;

            if(pingQueue.Count > 3)
            {
                Logger.LogWarning($"Player[{ClientId}] PingQueue reached maxmum count.");
            }
            if (TcpServer.Instance.TryGetClient(ClientId, out var client))
            {
                var serverTime = UnixTime.Now();
                Packet pingPacket = new Packet
                {
                    PacketType = PacketType.PING_REQUEST,
                    Payload = new S2CPingReq { ServerTime = serverTime },
                    Token = string.Empty,
                };
                client.EnqueueSend(pingPacket);
                pingQueue.Enqueue(serverTime);
            }
            else
            {   //연결이 끊겨도 여기서 발생함.
                Logger.LogError($"Player[{ClientId}] failed get TcpClient");
            }

            await UniTask.CompletedTask;
        }

        private async UniTask PongRes(long clientTime)
        {
            var serverTime = pingQueue.Dequeue();
            this.RoundTripTime = clientTime - serverTime;
            this.Latency = this.RoundTripTime / 2L;

            Logger.Log($"Player[{this.ClientId}] Pong! RTT : {this.RoundTripTime} / Latency : {this.Latency}");
            await UniTask.CompletedTask;
        }


        public static Player FromRedisData(int clientId, HashEntry[] redisValues)
        {
            return new Player(clientId)
            {
                UserName = (redisValues.First(x => x.Name == "user_name").Value).ToString(),
                IsReady = bool.Parse(redisValues.First(x => x.Name == "is_ready").Value),
                IsConnected = bool.Parse(redisValues.First(x => x.Name == "connected").Value),
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
                new HashEntry("connected", IsConnected.ToString()),
                new HashEntry("load_progress", LoadProgress.ToString()),
                new HashEntry("team_id", TeamId.ToString()),
                new HashEntry("avatar_id", AvatarId.ToString())
            };
        }
    }
}
