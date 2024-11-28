using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.PackageManager;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;
namespace ResourceWar.Server
{
    public class Player : IDisposable
    {
        
        public  int ClientId {get; private set;}
        private readonly string hashCode;
        private bool disposedValue;
       
        private S2CPingReq pingReq = new S2CPingReq();
        private Packet pingPacket = new Packet
        {
            PacketType = PacketType.PING_REQUEST,
        };

        private long latency = 0L;
        private long roundTripTime = 0L;

        private Queue<long> pingQueue = new();
        public Player(int clientId)
        {
          //  Logger.Log($"New Player : {clientId}");
            this.ClientId = clientId;
            this.hashCode = this.GetHashCode().ToString();
            pingPacket.Payload = pingReq;
            IntervalManager.Instance.AddTask(hashCode, PingReq, 1.0f);
            EventDispatcher<(int, int), long>.Instance.Subscribe((this.ClientId, int.MaxValue + this.ClientId), PongRes);
        }

        /// <summary>
        /// 연결이 끊겼다 다시 접속되었을 경우 클라이언트 아이디를 수정할 필요가 있음
        /// </summary>
        /// <param name="clientId"></param>
        public void SetClientId(int clientId)
        {
            Logger.LogWarning($"Player change clientId {this.ClientId} -> {clientId}");
            this.ClientId = clientId;
        }

        /// <summary>
        /// 클라에 핑 보내기
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async UniTask PingReq(CancellationToken token)
        {
            if(TcpServer.Instance.TryGetClient(this.ClientId, out var clientHandler))
            {
                var serverTime = UnixTime.Now(); 
                pingReq.ServerTime = serverTime; 
                clientHandler.EnqueueSend(pingPacket);
                if(pingQueue.Count < 3)
                {
                    pingQueue.Enqueue(serverTime);
                }
            }
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 클라에서 온 퐁 메세지 처리
        /// </summary>
        /// <param name="clientTime"></param>
        /// <returns></returns>
        public async UniTask PongRes(long clientTime)
        {
            if(clientTime <= 0)
            {
                Logger.LogError($"Player[{this.ClientId}] received zero! : {clientTime}");
                clientTime = UnixTime.Now();
            }
            if(this.pingQueue.TryPeek(out var serverTime)) { 
            this.roundTripTime = clientTime - serverTime;
            this.latency = this.roundTripTime / 2L;
         //   Logger.Log($"Pong[{this.ClientId}] Pong! RTT : {this.roundTripTime} / Latency : {this.latency}");
            }
            else
            {
                Logger.LogError($"Player[{this.ClientId}] received pong but pingQueue empty");
            }

            await UniTask.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    IntervalManager.Instance.CancelAllTasksByKey(hashCode);
                    EventDispatcher<(int, int), long>.Instance.Unsubcribe((this.ClientId, int.MaxValue + this.ClientId), PongRes);
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~Player()
        // {
        //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //     Dispose(disposing: false);
        // }
        void IDisposable.Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
