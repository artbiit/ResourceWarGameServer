using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using StackExchange.Redis.KeyspaceIsolation;
using StackExchange.Redis;
namespace ResourceWar.Server
{
    public class RedisClient : IDisposable
    {
        private bool disposedValue;
        private ConnectionMultiplexer redisConnection;
        private IDatabase DB;
        private ISubscriber sub;
        public void Connect(string host, int port)
        {
            redisConnection = ConnectionMultiplexer.Connect($"{host}:{port},password=zhsthfTD1!");
            if(redisConnection.IsConnected)
            {
                DB = redisConnection.GetDatabase();
                sub = redisConnection.GetSubscriber();
            }
        }

        public void SetKey(string key, string value)
        {
            DB.StringSet(key, value);
        }

        public string GetKey(string key)
        {
           return DB.StringGet(key);
        }

        public void Disconnect()
        {
            if (redisConnection != null)
            {
                redisConnection.Dispose();
                redisConnection = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    Disconnect();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~RedisClient()
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
