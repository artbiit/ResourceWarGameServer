using Cysharp.Threading.Tasks;
using log4net.Repository.Hierarchy;
using Protocol;
using ResourceWar.Server.Lib;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class Player
    {

        public int ClientId;

        public string Nickname { get; set; }
        public bool IsReady { get; set; }

        public bool IsConnected { get; set; }
        public int LoadProgress { get; set; }
        public int AvatarId { get; set; }
        public bool isGatheringResource = false;
        public long isGatheringTime = 0;
        public int playerSpeed = 100;
        public Vector3 position = Vector3.zero;
        public int ActionType { get; set; }
        public int EquippedItem { get; set; }


        /// <summary>
        /// ms 단위 지연시간
        /// </summary>
        public long Latency { get; private set; }
        /// <summary>
        /// ms 단위 RTT
        /// </summary>
        public long RoundTripTime { get; private set; }

        private Queue<long> pingQueue = new();
        private string hashCode;
        CancellationToken pingToken;

        public Player(int clientId, string nickName)
        {
            this.Nickname = nickName;
            this.AvatarId = 1;
            IsReady = false;
            IsConnected = true;
            LoadProgress = 0;
            this.hashCode = this.GetHashCode().ToString();
            this.ActionType = 7001;
            this.EquippedItem = 1001;
            Connected(clientId);
        }

        public Vector3 ChangePosition(Vector3 position)
        {
            float distance = position.magnitude;
            if(distance > 6) // 플레이어 대쉬 판별
            {
                ActionType = 3;
            }
            else if (distance > 12) // 거리가 너무 차이날 때
            {
                this.position = position;
            }
            Logger.Log($"플레이어{this.Nickname}의 이동 전 위치는 : {this.position}");
            this.position += position / 500;
            Logger.Log($"플레이어{this.Nickname}의 이동 후 위치는 : {this.position}");
            this.position.x = (float)Math.Round(this.position.x, 2);
            this.position.y = (float)Math.Round(this.position.y, 2);n this.position;
        }

        public void ChangeAction(byte ActionType)
        {
            if (isGatheringResource)
            {
                if(UnixTime.Now() - isGatheringTime > 1000)
                {
                    //파괴된 자원 객체 스폰 필요
                }
                else
                {

                }
                this.isGatheringResource = false;
                this.ActionType = ActionType;
            }
            else
            {
                this.isGatheringTime = UnixTime.Now();
                this.isGatheringResource = true;
            }
            
        }

        public void ChangeArea(uint DestinationAreaType)
        {
            this.position = Vector3.one * 999; // 임의로 멀리 이동시킴
            //Enum이던 인메모리에 있는 맵이던 가져와서 바꿔줄 예정
        }

        public void Connected(int clientId)
        {
            Logger.Log($"Player is reconnected {this.ClientId} -> {clientId}");
            this.ClientId = clientId;
            this.IsConnected = true;
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
            if (token.IsCancellationRequested) return;

            if (pingQueue.Count > 10)
            {
                Logger.LogWarning($"Player[{ClientId}] PingQueue reached maxmum count.");
            }
            else if (TcpServer.Instance.TryGetClient(ClientId, out var client))
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
    }
}
