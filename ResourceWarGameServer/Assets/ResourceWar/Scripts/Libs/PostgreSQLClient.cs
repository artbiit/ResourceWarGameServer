using Cysharp.Threading.Tasks;
using Npgsql;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class PostgreSQLClient : Singleton<PostgreSQLClient>, IDisposable
    {
        private bool disposedValue; // 객체의 해제 상태를 추적하는 플래그
        private NpgsqlConnection connection; // PostgreSQL 연결 객체
        private readonly ConcurrentQueue<Func<Task>> taskQueue = new(); // 작업을 저장하는 큐
        private bool isProcessingQueue = false; // 작업 큐 처리 상태 플래그
        private string connectionFormat = "Host={0};Port={1};Database={2};Username={3};Password={4};Pooling=true;MinPoolSize=5;MaxPoolSize=20;";
        /// <summary>
        /// PostgresSQL 서버와 연결되어 있는지 나타냅니다.
        /// </summary>
        public bool IsConnected => connection?.State == ConnectionState.Open;

        /// <summary>
        /// 동기 방식으로 PostgreSQL 서버에 연결합니다.
        /// </summary>
        /// <param name="host">PostgreSQL 서버 호스트</param>
        /// <param name="port">PostgreSQL 서버 포트</param>
        /// <param name="database">데이터베이스 이름</param>
        /// <param name="user">사용자 이름</param>
        /// <param name="password">사용자 비밀번호</param>
        /// <returns>연결 성공 여부</returns>
        public bool Connect(string host, int port, string database, string user, string password)
        {
            // 연결 문자열 생성
            connection = new NpgsqlConnection(CreateConnectionString(host, port, database,user,password)); // 연결 객체 생성
            return connectionInit(); // 연결 초기화 및 상태 반환
        }

        /// <summary>
        /// 비동기 방식으로 PostgreSQL 서버에 연결합니다.
        /// </summary>
        /// <param name="host">PostgreSQL 서버 호스트</param>
        /// <param name="port">PostgreSQL 서버 포트</param>
        /// <param name="database">데이터베이스 이름</param>
        /// <param name="user">사용자 이름</param>
        /// <param name="password">사용자 비밀번호</param>
        /// <returns>연결 성공 여부</returns>
        public async UniTask<bool> ConnectAsync(string host, int port, string database, string user, string password)
        {
            // 연결 문자열 생성
            connection = new NpgsqlConnection(CreateConnectionString(host, port, database, user, password)); // 연결 객체 생성
            await connection.OpenAsync(); // PostgreSQL 서버에 비동기로 연결
            return connectionInit(); // 연결 초기화 및 상태 반환
        }

        private string CreateConnectionString(string host, int port, string database, string user, string password) => string.Format(connectionFormat, host, port, database, user, password);

        /// <summary>
        /// 연결 초기화. 연결 상태를 확인하고 로그를 기록합니다.
        /// </summary>
        /// <returns>연결 상태 (성공: true, 실패: false)</returns>
        private bool connectionInit()
        {
            if (connection.State == ConnectionState.Open)
            {
                Logger.Log("PostgresSQL client is connected"); // 연결 성공 로그 기록
                return true;
            } 
            else
            {
                connection = null; // 연결 실패 시 객체 초기화
                Logger.LogError("PostgreSQL client failed to connect"); // 연결 실패 로그 기록
                return false;
            }
        }

        /// <summary>
        /// 비동기 데이터베이스 작업을 실행하고 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="T">결과 데이터 타입</typeparam>
        /// <param name="dbOperation">데이터베이스 작업</param>
        /// <returns>작업 결과</returns>
        public UniTask<T> ExecuteAsync<T>(Func<NpgsqlConnection, Task<T>> dbOperation)
        {
            var tcs = new UniTaskCompletionSource<T>(); // 결과를 처리할 TaskCompletionSource 생성
            taskQueue.Enqueue(async () =>
            {
                try
                {
                    T result = await dbOperation(connection); // 작업 실행
                    tcs.TrySetResult(result); // 작업 성공 시 결과 설정
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex); // 작업 실패 시 예외 설정
                }
            });

            ProcessQueue(); // 작업 큐 처리 시작
            return tcs.Task;
        }

        /// <summary>
        /// 비동기 데이터베이스 작업을 실행합니다.
        /// </summary>
        /// <param name="dbOperation">데이터베이스 작업</param>
        /// <returns>작업 완료 Task</returns>
        public UniTask ExecuteAsync(Func<NpgsqlConnection, Task> dbOperation)
        {
            var tcs = new UniTaskCompletionSource(); // 작업 완료를 처리할 TaskCompletionSource 생성
            taskQueue.Enqueue(async () =>
            {
                try
                {
                    await dbOperation(connection); // 작업 실행
                    tcs.TrySetResult(); // 작업 성공 시 완료 설정
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex); // 작업 실패 시 예외 설정
                }
            });

            ProcessQueue();  // 작업 큐 처리 시작
            return tcs.Task;
        }

        /// <summary>
        /// 작업 큐에 있는 작업을 처리합니다.
        /// </summary>
        private void ProcessQueue()
        {
            if (isProcessingQueue) return; // 이미 처리 중이면 중단

            isProcessingQueue = true; // 처리 상태를 활성화

            UniTask.Void(async () =>
            {
                while (taskQueue.TryDequeue(out var task)) // 큐에서 작업을 가져옴
                {
                    try
                    {
                        await task(); // 작업 실행
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"PostgresSQLClient exception in ProcessQueue : {ex.Message}");
                    }
                }
                isProcessingQueue = false; // 처리 상태 비활성화
            });
        }

        /// <summary>
        /// PostgreSQL 서버와의 연결을 종료합니다.
        /// </summary>
        public void Disconnect()
        {
            if (connection != null)
            {
                connection.Close(); // 연결 종료
                connection.Dispose(); // 연결 객체 해제
                connection = null;  // 객체 초기화
            }
        }

        /// <summary>
        /// 객체를 해제합니다.
        /// </summary>
        /// <param name="disposing">관리 리소스 해제 여부</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disconnect();  // 연결 해제
                }
                disposedValue = true; // 해제 상태 업데이트
            }
        }

        /// <summary>
        /// IDisposable 인터페이스 구현. Dispose 호출 시 리소스 정리 수행.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(disposing: true); // Dispose 호출
            GC.SuppressFinalize(this); // 소멸자 호출 방지
        }
    }
}
