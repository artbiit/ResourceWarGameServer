using System;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Net.Sockets;

namespace ResourceWar.Utils
{
    /// <summary>
    /// 메시지 큐를 관리하는 클래스.
    /// 수신 및 송신 큐를 별도로 관리하고, 비동기로 메시지를 처리
    /// </summary>
    public class MessageQueue
    {
        // 클라이언트별 메시지 큐를 관리하는 Dictionary
        private ConcurrentDictionary<int, ClientQueue> clientQueues = new ConcurrentDictionary<int, ClientQueue>();

        /// <summary>
        /// 클라이언트의 송신 및 수신 큐를 포함하는 클래스.
        /// </summary>
        private class ClientQueue
        {
            // 수신 메시지를 저장하는 큐
            public ConcurrentQueue<Message> ReceiveQueue { get; } = new ConcurrentQueue<Message>();
            // 송신 메시지를 저장하는 큐
            public ConcurrentQueue<byte[]> SendQueue { get; } = new ConcurrentQueue<byte[]>();
            // 수신 메시지 처리 중인지 상태 플래그
            public bool ProcessingReceive { get; set; } = false;
            // 송신 메시지 처리 중인지 상태 플래그
            public bool ProcessingSend { get; set; } = false;
        }

        /// <summary>
        /// 수신 메시지를 나타내느 구조체
        /// PakcetType: 메시지의 유형(패킷 타입)
        /// Payload: 메시지의 실제 데이터
        /// </summary>
        public struct Message
        {
            public int PacketType;
            public byte[] Payload;
        }

        /// <summary>
        /// 클라이언트를 추가합니다.
        /// </summary>
        /// <param name="clientId">클라이언트의 ID</param>
        public void AddClient(int clientId)
        {
            if (!clientQueues.TryAdd(clientId, new ClientQueue())) {
                Logger.LogError($"Client with socketId {clientId} already exists.");
            }
        }

        /// <summary>
        /// 클라이언트 큐에 있는 해당 유저를 제거합니다.
        /// </summary>
        /// <param name="clientId">클라이언트의 ID</param>
        public void RemoveClient(int clientId)
        {
            if (!clientQueues.TryRemove(clientId, out _))
            {
                Logger.LogError($"Client with socketId {clientId} does not exist.");
            }
        }

        /// <summary>
        /// 수신 큐에 메시지를 추가합니다.
        /// </summary>
        /// <param name="packetType">패킷 타입</param>
        /// <param name="payload">메시지의 데이터</param>
        public void EnqueueReceive(int clientId, int packetType, byte[] payload)
        {
            if ( clientQueues.TryGetValue(clientId, out var clientQueue))
            {
                // 메시지를 수신 큐에 추가
                clientQueue.ReceiveQueue.Enqueue(new Message { PacketType = packetType, Payload = payload });
                
                // 수신 큐 처리 시작
                ProcessReceiveQueue(clientId);
            }
             else
            {
                Logger.LogError($"Client with socketId {clientId} not found.");
            }
            
        }

        /// <summary>
        /// 송신 큐에 메시지를 추가합니다.
        /// </summary>
        /// <param name="data">송신할 메시지의 데이터</param>
        public void EnqueueSend(int clientId, byte[] data)
        {
            if ( clientQueues.TryGetValue(clientId, out var clientQueue))
            {
                // 메시지를 송신 큐에 추가
                clientQueue.SendQueue.Enqueue(data);
                // 송신 큐 처리 시작
                ProcessSendQueue(clientId);
            } else
            {
                Logger.LogError($"Client with socketId {clientId} not found.");
            }       
        }

        /// <summary>
        /// 수신 큐를 처리합니다.
        /// 큐에서 메시지를 꺼내서 핸들러로 전달합니다.
        /// </summary>
        private async void ProcessReceiveQueue(int clientId)
        {
            if (!clientQueues.TryGetValue(clientId, out var clientQueue)) return;
            // 이미 처리 중이라면 종료
            if (clientQueue.ProcessingReceive) return;
            // 처리 상태 활성화
            clientQueue.ProcessingReceive = true;

            // 수신 큐에 메시지가 남아 있는 동안 처리
            while (clientQueue.ReceiveQueue.TryDequeue(out var message))
            {
                try
                {
                    // 메시지 핸들러 호출
                    await HandleMessage(clientId, message.PacketType, message.Payload);
                    
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing message for socketId {clientId}: {ex.Message}");
                }
            }

            // 처리 상태 비활성화
            clientQueue.ProcessingReceive = false;
        }
        
        /// <summary>
        /// 송신 큐를 처리합니다.
        /// 큐에서 메시지를 꺼내서 클라이언트로 전송합니다.
        /// </summary>
        private async void ProcessSendQueue(int clientId)
        {
            if (!clientQueues.TryGetValue(clientId, out var clientQueue)) return;
            // 이미 처리 중이라면 종료
            if (clientQueue.ProcessingSend) return;
            // 처리 상태 활성화
            clientQueue.ProcessingSend = true;

            // 송신 큐에 메시지가 남아 있는 동안 처리
            while (clientQueue.SendQueue.TryDequeue(out var data))
            {
                try
                {
                    // 메시지를 클라이언트로 전송
                    await SendToClient(clientId, data);
                } 
                catch (Exception ex)
                {
                    Logger.LogError($"Error sending message to socketId {clientId}: {ex.Message}");
                }
            }

            // 처리 상태 비활성화
            clientQueue.ProcessingSend = false;
        }

        /// <summary>
        /// 메시지를 처리하는 로직
        /// 패킷 타입에 따라 다른 처리를 수행
        /// </summary>
        /// <param name="packetType">패킷 타입</param>
        /// <param name="payload">메시지 데이터</param>
        /// <returns></returns>
        private Task HandleMessage(int clientId, int packetType, byte[] payload)
        {
            Logger.Log($"Processing message from socketId {clientId} with type: {packetType}");

            // 여기에 핸들러 로직 추가
            return Task.CompletedTask;
        }

        /// <summary>
        /// 클라이언트로 데이터를 전송하는 로직
        /// 실제 송신 로직 추가
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Task SendToClient(int clientId, byte[] data)
        {
            Logger.Log($"Sending data to socketId {clientId}: {Encoding.UTF8.GetString(data)}");

            // 실제 클라이언트 송신 로직 추가 (예: NetworkStream.WriteAsync)
            return Task.CompletedTask;
        }
    }
}
