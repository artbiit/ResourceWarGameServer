using Cysharp.Threading.Tasks;
using ResourceWar.Utils;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    /// <summary>
    /// 클라이언트와의 연결을 관리하는 클래스
    /// 메시지 송수신, 연결 해제 및 메시지 처리 로직을 포함
    /// </summary>
    public class ClientHandler
    {
        public int ClientId { get; } // 클라이언트 ID
        private readonly TcpClient tcpClient; // 클라이언트 소켓
        private readonly NetworkStream stream; // 클라이언트와의 데이터 송수신 스트림
        private readonly Action<int> onDisconnect; // 클라이언트 연결 해제 시 호출되는 콜백
        private readonly MessageQueue messageQueue; // 클라이언트별 메시지 큐

        private CancellationTokenSource cts = new(); // 작업 취소 토큰

        /// <summary>
        /// ClientHandler 생성자.
        /// </summary>
        /// <param name="clientId">클라이언트 ID</param>
        /// <param name="tcpClient">TCP 소켓 객체</param>
        /// <param name="onDisconnect">연결 해제 시 호출되는 콜백</param>
        public ClientHandler(int clientId, TcpClient tcpClient, Action<int> onDisconnect)
        {
            this.ClientId = clientId; // 클라이언트 ID 설정
            this.tcpClient = tcpClient; // 클라이언트 소켓 설정
            this.onDisconnect = onDisconnect; // 연결 해제 콜백 설정
            // this.stream = this.tcpClient.GetStream(); // 네트워크 스트림 초기화

            messageQueue = new MessageQueue(this);  // 메세지 큐를 초기화
        }

        /// <summary>
        /// 데이터 송수신 처리를 시작하는 곳
        /// </summary>
        public void StartHandling()
        {
            _ = HandleReceivingAsync(); // 수신 처리 시작
            _ = HandleSendingAsync(); // 전송 처리 시작
        }

        /// <summary>
        /// 클라이언트로부터 수신된 메시지를 처리하는 곳
        /// </summary>
        /// <param name="packetType">패킷 타입</param>
        /// <param name="payload">메시지 데이터</param>
        /// <returns></returns>
        public async Task HandleMessage(int packetType, byte[] payload)
        {
            Logger.Log($"Client {ClientId}: Received packetType {packetType}, Payload: {Encoding.UTF8.GetString(payload)}");

            // 처리 로직 추가 (예: 특정 작업 실행 - PacketParser 만들어야함)
            if (packetType == 1)
            {
                var response = Encoding.UTF8.GetBytes("Acknowledged");
                messageQueue.EnqueueSend(response); // 송신 큐에 메시지 추가
            }
        }

        /// <summary>
        /// 클라이언트로 데이터를 전송합니다.
        /// </summary>
        /// <param name="data">전송할 데이터</param>
        /// <returns></returns>
        public async Task SendToClient(byte[] data)
        {
            try
            {
                // 클라이언트로 데이터 송신
                await stream.WriteAsync(data, 0, data.Length, cts.Token);
                Logger.Log($"Client {ClientId}: Sent {Encoding.UTF8.GetString(data)}");
            } catch (Exception ex)
            {
                Logger.LogError($"Error sending to client {ClientId}: {ex.Message}");
                Disconnect();
            }
        }

        /// <summary>
        /// 클라이언트로부터 데이터를 비동기로 수신합니다.
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid HandleReceivingAsync()
        {
            byte[] buffer = new byte[1024]; // 데이터 수신을 위한 버퍼
            int PACKET_TYPE_LENGTH = 2; // 패킷 타입 길이 (바이트)
            int PACKET_TOKEN_LENGTH = 1; // 토큰 길이 (바이트)
            int PACKET_PAYLOAD_LENGTH = 4; // 페이로드 길이 (바이트)
            int PACKET_TOTAL_LENGTH = PACKET_TYPE_LENGTH + PACKET_TOKEN_LENGTH + PACKET_PAYLOAD_LENGTH; // 전체 패킷 길이
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 클라이언트로부터 데이터 읽기
                    int byteRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    // 수신된 데이터 처리 후 대기열에 추가 (현재는 패킷 파싱 필요)
                    if (byteRead > 0)
                    {
                        int offset = 0;
                        // 패킷이 충분한 길이인지 확인
                        while (byteRead - offset >= PACKET_TOKEN_LENGTH)
                        {
                            // 패킷 타입 추출
                            int packetType = BitConverter.ToUInt16(buffer, offset); // 2바이트 읽음
                            offset += PACKET_TYPE_LENGTH;

                            // 토큰 길이 추출
                            byte tokenLength = buffer[offset]; // 1바이트 읽음
                            offset += PACKET_TOKEN_LENGTH;

                            // 토큰 데이터 추출
                            string token = Encoding.UTF8.GetString(buffer, offset, tokenLength); // 토큰 문자열
                            offset += tokenLength;

                            // 페이로드 길이 추출
                            int payloadLength = BitConverter.ToInt32(buffer, offset); // 4바이트 읽음
                            offset += PACKET_PAYLOAD_LENGTH;

                            // 페이로드 데이터 추출
                            if (byteRead - offset >= payloadLength)
                            {
                                byte[] payload = new byte[payloadLength];
                                Array.Copy(buffer, offset, payload, 0, payloadLength);
                                offset += payloadLength;

                                // 메시지 큐에 수신 데이터 추가
                                messageQueue.EnqueuReceive(packetType, payload);
                            }
                            else
                            {
                                Logger.LogError("Incomplete packet received. Waiting for more data.");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Logger.LogError($"{nameof(ClientHandler)}/{nameof(HandleReceivingAsync)} Error in receiving from client {ClientId}: {ex.Message}");
                Disconnect();
            }
            finally
            {

            }

        }

        /// <summary>
        /// 메시지 큐에서 데이터를 꺼내 클라이언트로 송신
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid HandleSendingAsync()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 메시지 큐에서 전송 처리하는 messageQueue 내부에서 관리중

                    // 짧은 대기 (CPU 과부하 방지)
                    await UniTask.Delay(10, cancellationToken: cts.Token);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"{nameof(ClientHandler)}/{nameof(HandleSendingAsync)} Error in sending to client {ClientId}: {ex.Message}");
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
            onDisconnect?.Invoke(ClientId); // 연결 해제 콜백함수 호출
        }
    }
}
