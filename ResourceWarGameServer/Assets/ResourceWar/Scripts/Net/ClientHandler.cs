using Cysharp.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class ClientHandler
    {
        private readonly int clientId; // 클라이언트 ID
        private readonly TcpClient tcpClient; // 클라이언트 소켓
        private readonly NetworkStream stream; // 네트워크 스트림
        private readonly ConcurrentQueue<string> sendQueue = new(); // 전송 대기열
        private readonly ConcurrentQueue<string> receiveQueue = new(); // 수신 대기열
        private readonly Action<int> onDisconnect; // 클라이언트 연결 해제 시 호출되는 콜백

        private CancellationTokenSource cts = new(); // 작업 취소 토큰

        public ClientHandler(int clientId, TcpClient tcpClient, Action<int> onDisconnect)
        {
            this.clientId = clientId; // 클라이언트 ID 설정
            this.tcpClient = tcpClient; // 클라이언트 소켓 설정
            this.onDisconnect = onDisconnect; // 연결 해제 콜백 설정
            this.stream = this.tcpClient.GetStream(); // 네트워크 스트림 초기화
        }

        // 데이터 처리 시작
        public void StartHandling()
        {
            _ = HandleReceivingAsync(); // 수신 처리 시작
            _ = HandleSendingAsync(); // 전송 처리 시작
        }

        // 클라이언트로부터 데이터 수신 처리
        public async UniTaskVoid HandleReceivingAsync()
        {
            byte[] buffer = new byte[1024]; // 데이터 수신 버퍼
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 클라이언트로부터 데이터 읽기
                    int byteRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    // 수신된 데이터 처리 후 대기열에 추가 (현재는 패킷 파싱 필요)
                    receiveQueue.Enqueue("여기에 패킷 파싱해서 넣어야함");
                }
            }
            catch (Exception ex)
            {

                Logger.LogError($"{nameof(ClientHandler)}/{nameof(HandleReceivingAsync)} Error in receiving from client {this.clientId}: {ex.Message}");
            }
            finally
            {

            }

        }

        // 클라이언트로 데이터 전송 처리
        private async UniTaskVoid HandleSendingAsync()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 전송 대기열에 메시지가 있으면 클라이언트로 전송
                    while (sendQueue.TryDequeue(out var message))
                    {
                        // 메시지 전송 로직 필요
                        //여기다가 전송 넣어야함
                        //await stream.WriteAsync()
                    }

                    // 짧은 대기 (CPU 과부하 방지)
                    await UniTask.Delay(10, cancellationToken: cts.Token);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(ClientHandler)}/{nameof(HandleSendingAsync)} Error in sending to client {this.clientId}: {ex.Message}");
            }
            finally
            {

            }
        }

        // 클라이언트 연결 해제
        public void Disconnect()
        {
            cts?.Cancel(); // 작업 취소
            stream?.Close(); // 스트림 취소
            tcpClient?.Close(); // 클라이언트 소켓 종료
            onDisconnect?.Invoke(clientId); // 연결 해제 콜백함수 호출
        }
    }
}
