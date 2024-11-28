using System;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using ResourceWar.Server.Lib;
using System.Reflection;
namespace ResourceWar.Server
{
    public class RedisClient :  Singleton<RedisClient>, IDisposable
    {
        private bool disposedValue;
        private ConnectionMultiplexer redisConnection;
        private IDatabase db;
        private ISubscriber subscriber;
        public ISubscriber Subscriber => subscriber;
        private readonly ConcurrentQueue<System.Func<Task>> taskQueue = new();
        private bool isProcessingQueue = false;

        /// <summary>
        /// 레디스 서버와 연결되어 있는지 나타냅니다.
        /// </summary>
        public bool isConnected => this.redisConnection?.IsConnected ?? false;

        public bool Connect(string host, int port,string password)
        {
            redisConnection = ConnectionMultiplexer.Connect(CreateConnectionUrl(host, port, password));
            return connectionInit();
        }
        public async UniTask<bool> ConnectAsync(string host, int port, string password)
        {
            redisConnection = await ConnectionMultiplexer.ConnectAsync(CreateConnectionUrl(host, port, password));
            return connectionInit();
        }

        private string CreateConnectionUrl(string host, int port, string password) => $"{host}:{port},password={password}";

        private bool connectionInit()
        {
            if (redisConnection.IsConnected)
            {
                db = redisConnection.GetDatabase();
                subscriber = redisConnection.GetSubscriber();
                Logger.Log("Redis client is connected");
                return true;
            }
            else
            {
                db = null;
                subscriber = null;
                redisConnection = null;
                Logger.LogError("Redis client failed to connect");
                return false;
            }
        }


        public UniTask<T> ExecuteAsync<T>(Func<IDatabase, Task<T>> redisOperation)
        {
            var tcs = new UniTaskCompletionSource<T>();
            taskQueue.Enqueue(async () =>
            {
                try
                {
                    T result = await redisOperation(db);
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            ProcessQueue();
            return tcs.Task;
        }

        public UniTask ExecuteAsync(Func<IDatabase, Task> redisOperation)
        {
            var tcs = new UniTaskCompletionSource();
            taskQueue.Enqueue(async () =>
            {
                try
                {
                    await redisOperation(db);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            ProcessQueue();
            return tcs.Task;
        }


        private void ProcessQueue()
        {
            if (isProcessingQueue) return;

            isProcessingQueue = true;

            UniTask.Void(async () =>
            {
                while (taskQueue.TryDequeue(out var task))
                {
                    try
                    {
                        await task();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"RedisClient exceiption in ProcessQueue : {ex.Message}");
                    }
                }
                isProcessingQueue = false;
            });
        }

        /// <summary>
        /// 객체를 Redis 해시에 저장
        /// </summary>
        public UniTask SaveObjectToHash<T>(string key, T obj)
        {
            return ExecuteAsync(async db =>
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var hashEntries = new HashEntry[properties.Length];
                int index = 0;

                foreach (var property in properties)
                {
                    var value = property.GetValue(obj)?.ToString() ?? string.Empty;
                    hashEntries[index++] = new HashEntry(property.Name, value);
                }

                await db.HashSetAsync(key, hashEntries);
            });
        }

        /// <summary>
        /// Redis 해시에서 객체를 읽어오기
        /// </summary>
        public UniTask<T> LoadObjectFromHash<T>(string key) where T : new()
        {
            return ExecuteAsync(async db =>
            {
                var hashEntries = await db.HashGetAllAsync(key);
                if (hashEntries.Length == 0)
                {
                    return default;
                }

                var obj = new T();
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    var entry = Array.Find(hashEntries, x => x.Name == property.Name);
                    if (entry.Name.HasValue)
                    {
                        var value = Convert.ChangeType(entry.Value.ToString(), property.PropertyType);
                        property.SetValue(obj, value);
                    }
                }

                return obj;
            });
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
                this.db = null;
                this.subscriber = null;
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
