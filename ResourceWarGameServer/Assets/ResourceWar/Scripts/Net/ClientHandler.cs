using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.Experimental.AI;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    /// <summary>
    /// 클라이언트와의 연결을 관리하는 클래스
    /// 메시지 송수신, 연결 해제 및 메시지 처리 로직을 포함
    /// </summary>
    public class ClientHandler : IDisposable
    {
        public string Token { get; private set; }
        private readonly int clientId; // 클라이언트 ID
        private readonly TcpClient tcpClient; // 클라이언트 소켓
        private readonly NetworkStream stream; // 클라이언트와의 데이터 송수신 스트림
        private readonly Action<int> onDisconnect; // 클라이언트 연결 해제 시 호출되는 콜백

        private CancellationTokenSource cts = new(); // 비동기 작업 취소 토큰
        private readonly Queue<ReceivedPacket> receiveQueue = new(); // 수신 큐
        private readonly Queue<Packet> sendQueue = new(); // 송신 큐

        private readonly MemoryStream receiveBuffer = new MemoryStream();
        private bool isProcessingReceive = false; // 수신 큐 처리 여부 플래그
        private bool isProcessingSend = false; // 송신 큐 처리 여부 플래그
        private bool disposedValue;
        /// <summary>
        /// 인증된 클라이언트인지 검사
        /// </summary>
        public bool IsAuthorized { get; private set; } = false;


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
        }

        /// <summary>
        /// 수신 큐에 패킷 추가 및 처리 시작
        /// </summary>
        private void EnqueueReceive(ReceivedPacket packet)
        {
            receiveQueue.Enqueue(packet); // 수신 큐에 시작
            Logger.Log($"[ReceiveQueue] Enqueued packet: Type={packet.PacketType}, Token={packet.Token}, Payload= {packet.Payload}, clientId = {packet.ClientId}");
            _ = ProcessReceiveQueue(); // 수신 큐  처리 시작
        }

        public void EnqueueSend(Packet packet)
        {
            sendQueue.Enqueue(packet);
            _ = ProcessSendingQueue();
        }

        /// <summary>
        /// 클라이언트로부터 데이터를 비동기로 수신합니다.
        /// </summary>
        private async UniTaskVoid HandleReceivingAsync()
        {
            byte[] buffer = new byte[1024];
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    if (bytesRead > 0)
                    {
                        // 받은 데이터를 버퍼에 추가
                        receiveBuffer.Write(buffer, 0, bytesRead);
                        ProcessBufferedData();
                    }
                }
                catch (IOException e)
                {
                    Logger.LogError($"HandleReceivingAsync[{clientId}] {e.Message}");
                    Disconnect();
                }
                catch (Exception ex)
                {
                    receiveBuffer.SetLength(0);
                    Logger.LogError($"Error receiving data: {ex.Message}");

                }
            }
        }

        /// <summary>
        /// 클라이언트로 데이터를 비동기로 송신
        /// </summary>
        private async UniTaskVoid ProcessSendingQueue()
        {
            if (isProcessingSend)
            {
                return;
            }
            isProcessingSend = true;


            while (sendQueue.TryDequeue(out var packet))
            {
                try
                {
                    var data = packet.ToBytes(); // 패킷 데이터를 바이트로 변환
                    await stream.WriteAsync(data, 0, data.Length, cts.Token); // 데이터 송신

                    // Protobuf 메시지를 JSON 문자열로 변환
                    string payloadString = packet.Payload.ToString();
                    Logger.Log($"[SendQueue] Dequeued and sent packet: Type={packet.PacketType}, Token={packet.Token}, Payload={payloadString}");
                }
                catch (IOException e)
                {
                    Logger.LogError($"HandleSendingAsync[{clientId}] {e.Message}");
                    Disconnect();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error sending data to client {clientId}, {ex.StackTrace}");

                }
            }

            await UniTask.NextFrame(PlayerLoopTiming.LastPreUpdate);
            isProcessingSend = false;
        }

        /// <summary>
        /// 수신 큐에서 패킷을 처리
        /// </summary>
        private async UniTaskVoid ProcessReceiveQueue()
        {
            if (isProcessingReceive) return; // 이미 처리 중이면 중복 실행 방지
            isProcessingReceive = true;
            int needAuthCount = 0;
            while (receiveQueue.TryDequeue(out var packet))
            {
                try
                {
                    // Protobuf 메시지를 JSON 문자열로 변환

                    //   Logger.Log($"[ReceiveQueue] Dequeued packet: Type={packet.PacketType}, Token={packet.Token}, Timestamp={packet.Timestamp}, Payload={payloadString}");

                    if (IsAuthorized == false && packet.PacketType != PacketType.AUTHORIZE_REQUEST)
                    {
                        if(needAuthCount <= 3) { 
                        var needAuthNoti = new Packet
                        {
                            PacketType = PacketType.NEED_AUTHORIZE,
                            Payload = new S2CNeedAuthorizeNoti(),
                            Token = "",
                        };
                        EnqueueSend(needAuthNoti);
                        }
                        else
                        {
                            throw new Exception($"Authorize failed : {clientId}");
                        }
                        needAuthCount++;
                    }
                    else
                    {
                        var resultPacket = await MessageHandlers.Instance.ExecuteHandler(packet);
                        if (resultPacket != null)
                        {
                            EnqueueSend(resultPacket);
                        }
                    }
                    // 메시지 핸들러 호출 또는 추가 로직 처리
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing packet: {ex.Message}");  
                }
            }
            await UniTask.NextFrame(PlayerLoopTiming.LastPreUpdate);
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

        private void ProcessBufferedData()
        {
            receiveBuffer.Position = 0; // 스트림의 시작점으로 이동

            while (receiveBuffer.Length - receiveBuffer.Position >= 7) // 최소 패킷 헤더 크기 확인
            {
                long startPosition = receiveBuffer.Position;

                // 패킷 크기 계산
                try
                {
                    using var reader = new BinaryReader(receiveBuffer, Encoding.UTF8, leaveOpen: true);

                    var packetType = (PacketType)PacketUtils.ReadUInt16BigEndian(reader);
                    byte tokenLength = reader.ReadByte();

                    if (receiveBuffer.Length - receiveBuffer.Position < tokenLength + 4)
                    {
                        receiveBuffer.Position = startPosition;
                        break; // 데이터가 부족하면 대기
                    }

                    string token = Encoding.UTF8.GetString(reader.ReadBytes(tokenLength));
                    int payloadLength = PacketUtils.ReadInt32BigEndian(reader);
                    if (receiveBuffer.Length - receiveBuffer.Position < payloadLength)
                    {
                        receiveBuffer.Position = startPosition;
                        break; // 데이터가 부족하면 대기
                    }

                    byte[] payloadBytes = reader.ReadBytes(payloadLength);
                    // 패킷 생성 및 큐 추가
                    var protoMessages = PacketUtils.CreateMessage(packetType); // 패킷 타입에 맞는 Protobuf 메시지 검색
                    IMessage payload = protoMessages.Descriptor.Parser.ParseFrom(payloadBytes); // 페이로드 파싱
                    var packet = new ReceivedPacket(this.clientId) { PacketType = packetType, Payload = payload, Token = token };

                    if (packet != null)
                    {
                        EnqueueReceive(packet);
                    }
                    else
                    {
                        Logger.Log("Packet is null");
                    }



                }
                catch (Exception e)
                {

                    Logger.LogError(e);
                    // 잘못된 데이터가 있으면 남은 데이터 대기
                    receiveBuffer.Position = startPosition;
                    break;
                }
            }

            // 남은 데이터를 임시 버퍼에 보관
            var remainingData = receiveBuffer.Length - receiveBuffer.Position;
            if (remainingData > 0)
            {
                byte[] tempBuffer = new byte[remainingData];
                receiveBuffer.Read(tempBuffer, 0, tempBuffer.Length);
                receiveBuffer.SetLength(0); // 기존 스트림 초기화
                receiveBuffer.Write(tempBuffer, 0, tempBuffer.Length); // 남은 데이터 저장
            }
            else
            {
                receiveBuffer.SetLength(0); // 남은 데이터가 없으면 스트림 초기화
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    receiveBuffer.Dispose();
                    Disconnect();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~ClientHandler()
        // {
        //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //     Dispose(disposing: false);
        // }

        public void Authorized()
        {
            Logger.Log($"Client[{clientId}] authorized.");
            IsAuthorized = true;
        }
        void IDisposable.Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
