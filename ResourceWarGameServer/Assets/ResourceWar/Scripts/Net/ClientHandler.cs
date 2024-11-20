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
            this.stream = this.tcpClient.GetStream(); // 네트워크 스트림 초기화

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
            try
            {
                Logger.Log($"Client {ClientId}: Received packetType {packetType}, Raw Payload Bytes: {BitConverter.ToString(payload)}");

                // UTF-8 문자열로 변환
                string payloadString = Encoding.UTF8.GetString(payload);
                Logger.Log($"Client {ClientId}: Parsed Payload: {payloadString}");

                // 처리 로직
                if (packetType != -1)
                {
                    var response = Encoding.UTF8.GetBytes("Acknowledged");
                    messageQueue.EnqueueSend(response); // 송신 큐에 메시지 추가
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in HandleMessage: {ex.Message}");
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
                if (tcpClient == null || !tcpClient.Connected || stream == null)
                {
                    Logger.LogError($"Client {ClientId}: Cannot send data. Connection is closed.");
                    Disconnect();
                    return;
                }
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
            // 수신 데이터 누적 버퍼
            var accumulatedBuffer = new MemoryStream();
            byte[] buffer = new byte[1024]; // 읽기 버퍼
            const int PACKET_TYPE_LENGTH = 2; // 패킷 타입 길이
            const int PACKET_PAYLOAD_LENGTH = 4; // 페이로드 길이

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 클라이언트로부터 데이터 읽기
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead > 0)
                    {
                        // 새 데이터를 누적 버퍼에 추가
                        accumulatedBuffer.Write(buffer, 0, bytesRead);

                        while (accumulatedBuffer.Length >= PACKET_TYPE_LENGTH + PACKET_PAYLOAD_LENGTH)
                        {
                            // 누적 데이터를 배열로 변환
                            byte[] data = accumulatedBuffer.ToArray();

                            // 패킷 타입 추출
                            int packetType = BitConverter.ToUInt16(data, 0);
                            int offset = PACKET_TYPE_LENGTH;

                            // 페이로드 길이 추출
                            int payloadLength = BitConverter.ToInt32(data, offset);
                            offset += PACKET_PAYLOAD_LENGTH;

                            // 디버깅 로그: 패킷 타입 및 페이로드 길이
                            Logger.Log($"Client {ClientId}: Parsed packetType = {packetType}, payloadLength = {payloadLength}");

                            /*// 페이로드 크기 제한 검사
                            if (payloadLength > 10 * 1024 * 1024) // 예: 10MB 제한
                            {
                                Logger.LogError($"Client {ClientId}: Payload size exceeds the maximum allowed size ({payloadLength} bytes).");
                                Disconnect();
                                return;
                            }*/

                            // 패킷 전체 크기 계산
                            int totalPacketSize = offset + payloadLength;

                            // 패킷이 완전히 도착했는지 확인
                            if (accumulatedBuffer.Length >= totalPacketSize)
                            {
                                // 페이로드 데이터 추출
                                byte[] payload = new byte[payloadLength];
                                Array.Copy(data, offset, payload, 0, payloadLength);

                                // 디버깅 로그: 페이로드 바이트 및 문자열
                                Logger.Log($"Client {ClientId}: Extracted payload bytes = {BitConverter.ToString(payload)}");
                                string payloadString = Encoding.UTF8.GetString(payload);
                                Logger.Log($"Client {ClientId}: Parsed payload string = {payloadString}");

                                // 메시지 큐에 수신 데이터 추가
                                messageQueue.EnqueuReceive(packetType, payload);

                                // 사용한 데이터를 누적 버퍼에서 제거
                                byte[] remainingData = new byte[accumulatedBuffer.Length - totalPacketSize];
                                Array.Copy(data, totalPacketSize, remainingData, 0, remainingData.Length);

                                accumulatedBuffer.SetLength(0); // 버퍼 초기화
                                accumulatedBuffer.Write(remainingData, 0, remainingData.Length);
                            }
                            else
                            {
                                // 패킷이 완전히 도착하지 않음, 다음 ReadAsync에서 기다림
                                Logger.Log($"Client {ClientId}: Incomplete packet received. Waiting for more data.");
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
        }


        /// <summary>
        /// 메시지 큐에서 데이터를 꺼내 클라이언트로 송신
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid HandleSendingAsync()
        {
            // 수신 데이터 누적 버퍼
            var accumulatedBuffer = new MemoryStream();
            byte[] buffer = new byte[1024]; // 읽기 버퍼
            const int PACKET_TYPE_LENGTH = 2; // 패킷 타입 길이
            const int PACKET_PAYLOAD_LENGTH = 4; // 페이로드 길이
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // 클라이언트로부터 데이터 읽기
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

                    if (bytesRead > 0)
                    {
                        // 새 데이터를 누적 버퍼에 추가
                        accumulatedBuffer.Write(buffer, 0, bytesRead);

                        while (accumulatedBuffer.Length >= PACKET_TYPE_LENGTH + PACKET_PAYLOAD_LENGTH)
                        {
                            // 누적 데이터를 배열로 변환
                            byte[] data = accumulatedBuffer.ToArray();

                            // 패킷 타입 추출
                            int packetType = BitConverter.ToUInt16(data, 0);
                            int offset = PACKET_TYPE_LENGTH;

                            // 페이로드 길이 추출
                            int payloadLength = BitConverter.ToInt32(data, offset);
                            offset += PACKET_PAYLOAD_LENGTH;

                            // 패킷 전체 크기 계산
                            int totalPacketSize = offset + payloadLength;

                            // 패킷이 완전히 도착했는지 확인
                            if (accumulatedBuffer.Length >= totalPacketSize)
                            {
                                // 페이로드 데이터 추출
                                byte[] payload = new byte[payloadLength];
                                Array.Copy(data, offset, payload, 0, payloadLength);

                                // 메시지 큐에 수신 데이터 추가
                                messageQueue.EnqueuReceive(packetType, payload);

                                // 사용한 데이터를 누적 버퍼에서 제거
                                byte[] remainingData = new byte[accumulatedBuffer.Length - totalPacketSize];
                                Array.Copy(data, totalPacketSize, remainingData, 0, remainingData.Length);

                                accumulatedBuffer.SetLength(0); // 버퍼 초기화
                                accumulatedBuffer.Write(remainingData, 0, remainingData.Length);
                            }
                            else
                            {
                                // 패킷이 완전히 도착하지 않음, 다음 ReadAsync에서 기다림
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
