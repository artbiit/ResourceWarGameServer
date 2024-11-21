using Cysharp.Threading.Tasks;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    /// <summary>
    /// 클라이언트와의 연결을 관리하는 클래스
    /// 메시지 송수신, 연결 해제 및 메시지 처리 로직을 포함
    /// </summary>
    public class ClientHandler
    {
        private readonly int clientId; // 클라이언트 ID
        private readonly TcpClient tcpClient; // 클라이언트 소켓
        private readonly NetworkStream stream; // 클라이언트와의 데이터 송수신 스트림
        private readonly Action<int> onDisconnect; // 클라이언트 연결 해제 시 호출되는 콜백

        private CancellationTokenSource cts = new(); // 비동기 작업 취소 토큰
        private readonly ConcurrentQueue<Packet> receiveQueue = new(); // 수신 큐
        private readonly ConcurrentQueue<Packet> sendQueue = new(); // 송신 큐

        private bool isProcessingReceive = false; // 수신 큐 처리 여부 플래그

        public ClientHandler(int clientId, TcpClient tcpClient, Action<int> onDisconnect)
        {
            this.clientId = clientId; // 클라이언트 ID 설정
            this.tcpClient = tcpClient; // 클라이언트 소켓 설정
            this.onDisconnect = onDisconnect; // 연결 해제 콜백 설정
            this.stream = this.tcpClient.GetStream(); // 네트워크 스트림 초기화
        }

        /// <summary>
        /// 데이터 송수신 처리를 시작하는 곳
        /// </summary>
        public void StartHandling()
        {
            HandleReceivingAsync().Forget(); // 수신 처리 비동기 시작
            HandleSendingAsync().Forget(); // 송신 처리 비동기 시작
        }

        /// <summary>
        /// 수신 큐에 패킷 추가 및 처리 시작
        /// </summary>
        private void EnqueueReceive(Packet packet)
        {
            packet.Timestamp = DateTime.UtcNow; // 수신 시점 기록
            receiveQueue.Enqueue(packet); // 수신 큐에 시작
            Logger.Log($"[ReceiveQueue] Enqueued packet: Type={packet.PacketType}, Token={packet.Token}, Payload= {packet.Payload}, Timestamp={packet.Timestamp}");
            ProcessReceiveQueue(); // 수신 큐  처리 시작
        }

        /// <summary>
        /// 송신 큐에 패킷 추가
        /// </summary>
        public void EnqueueSend<T>(ushort packetType, string token, T payload) where T : IMessage
        {
            var packet = new Packet
            {
                PacketType = packetType,
                Token = token,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };
            sendQueue.Enqueue(packet); // 송신 큐에 추가

            // Protobuf 메시지를 JSON 문자열로 변환
            string payloadString = payload.ToString();
            Logger.Log($"[SendQueue] Enqueued packet: Type={packet.PacketType}, Token={packet.Token}, Timestamp={packet.Timestamp}, Payload={payloadString}");
        }

        /// <summary>
        /// 클라이언트로부터 데이터를 비동기로 수신합니다.
        /// </summary>
        private async UniTaskVoid HandleReceivingAsync()
        {
            byte[] buffer = new byte[1024]; // 읽기 버퍼

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token); // 데이터 읽기
                    if (bytesRead > 0)
                    {
                        var memoryStream = new MemoryStream(buffer, 0, bytesRead); // 받은 데이터를 메모리 스트림으로 처리
                        while (memoryStream.Length >= 8) // 최소 패킷 헤더 크기 (패킷 타입 + 토큰 길이 + 페이로드 길이)
                        {
                            var packet = Packet.FromStream(memoryStream); // 패킷 파싱
                            if (packet != null)
                            {
                                EnqueueReceive(packet); // 수신 큐에 추가
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error receiving data from client {clientId}: {ex.Message}");
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// 클라이언트로 데이터를 비동기로 송신
        /// </summary>
        private async UniTaskVoid HandleSendingAsync()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (sendQueue.TryDequeue(out var packet))
                {
                    try
                    {
                        var data = packet.ToBytes(); // 패킷 데이터를 바이트로 변환
                        await stream.WriteAsync(data, 0, data.Length, cts.Token); // 데이터 송신

                        // Protobuf 메시지를 JSON 문자열로 변환
                        string payloadString = packet.Payload.ToString();
                        Logger.Log($"[SendQueue] Dequeued and sent packet: Type={packet.PacketType}, Token={packet.Token}, Timestamp={packet.Timestamp}, Payload={payloadString}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error sending data to client {clientId}: {ex.Message}");
                        Disconnect();
                    }
                }
                await UniTask.Delay(10); // 송신 작업 간 짧은 대기
            }
        }

        /// <summary>
        /// 수신 큐에서 패킷을 처리
        /// </summary>
        private void ProcessReceiveQueue()
        {
            if (isProcessingReceive) return; // 이미 처리 중이면 중복 실행 방지
            isProcessingReceive = true;

            while (receiveQueue.TryDequeue(out var packet))
            {
                try
                {
                    // Protobuf 메시지를 JSON 문자열로 변환
                    string payloadString = packet.Payload.ToString();
                    Logger.Log($"[ReceiveQueue] Dequeued packet: Type={packet.PacketType}, Token={packet.Token}, Timestamp={packet.Timestamp}, Payload={payloadString}");

                    // 메시지 핸들러 호출 또는 추가 로직 처리
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing packet: {ex.Message}");
                }
            }

            isProcessingReceive = false;
        }

        // 클라이언트 연결 해제
        public void Disconnect()
        {
            cts?.Cancel(); // 작업 취소
            stream?.Close(); // 스트림 취소
            tcpClient?.Close(); // 클라이언트 소켓 종료
            onDisconnect?.Invoke(clientId); // 연결 해제 콜백함수 호출
        }

        public class Packet
        {
            public ushort PacketType { get; set; }
            public string Token { get; set; }
            public IMessage Payload { get; set; } // Protobuf 메시지
            public DateTime Timestamp { get; set; } // 패킷 생성 또는 송수신 시점
            
            /// <summary>
            /// 스트림에서 패킷 읽기
            /// </summary>
            public static Packet FromStream(Stream stream)
            {
                // 데이터 읽기 위한 BinaryReader
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                try
                {
                    ushort packetType = reader.ReadUInt16(); // 패킷 타입 읽기
                    int tokenLength = reader.ReadByte(); // 토큰 길이 읽기
                    string token = Encoding.UTF8.GetString(reader.ReadBytes(tokenLength)); // 토큰 데이터 읽기
                    int payloadLength = reader.ReadInt32(); // 페이로드 길이 읽기
                    byte[] payloadBytes = reader.ReadBytes(payloadLength);

                    var protoMessages = ProtoMessageRegistry.GetMessage(packetType); // 패킷 타입에 맞는 Protobuf 메시지 검색
                    IMessage payload = protoMessages?.Descriptor.Parser.ParseFrom(payloadBytes); // 페이로드 파싱

                    return new Packet { PacketType = packetType, Token = token, Payload = payload };
                } catch
                {
                    return null; // 데이터가 부족하거나 잘못된 경우 null 반환 (반드시 형식을 맞춰 보내야함)
                }
            }

            /// <summary>
            /// 패킷 데이터를 바이트 배열로 반환
            /// </summary>

            public byte[] ToBytes()
            {
                var tokenBytes = Encoding.UTF8.GetBytes(Token); // 토큰 데이터를 바이트로 변환
                var payloadBytes = Payload.ToByteArray();

                using var stream = new MemoryStream(); // 메모리 스트림 생성
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true); // BinaryWriter로 데이터를 쓰기
                writer.Write(PacketType); // 패킷 타입 쓰기
                writer.Write((byte)tokenBytes.Length); // 토큰 길이 쓰기
                writer.Write(tokenBytes); // 토큰 데이터 쓰기
                writer.Write(payloadBytes.Length); // 페이로드 길이 쓰기
                writer.Write(payloadBytes); // 페이로드 데이터 쓰기

                return stream.ToArray(); // 스트림 내용을 바이트 배열로 반환
            }
        }
    }
}
