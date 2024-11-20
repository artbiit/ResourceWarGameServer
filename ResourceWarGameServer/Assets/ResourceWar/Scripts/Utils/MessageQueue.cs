using System;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text;
using Logger = ResourceWar.Server.Lib.Logger;
using System.Net.Sockets;
using ResourceWar.Server;
using Codice.CM.Common.Checkin.Partial;

namespace ResourceWar.Utils
{
    /// <summary>
    /// 메시지 큐를 관리하는 클래스.
    /// 특정 클라이언트의 송신 및 수신 큐를 관리하고 처리.
    /// </summary>
    public class MessageQueue
    {
        private readonly ConcurrentDictionary<int, ClientQueue> clientQueues = new();

        public struct Message
        {
            public int PacketType;
            public byte[] Payload;
        }

        private class ClientQueue
        {
            public ClientHandler Handler { get; set; }
            public ConcurrentQueue<Message> ReceiveQueue { get; } = new();
            public ConcurrentQueue<byte[]> SendQueue { get; } = new();
            public bool isProcessingReceive { get; set; } = false;
            public bool isProcessingSend { get; set; } = false;
        }

        public void AddClient(int clientId, ClientHandler handler)
        {
            if (!clientQueues.TryAdd(clientId, new ClientQueue { Handler = handler}))
            {
                Logger.LogError($"Client with ID {clientId} already exists.");
            }
        }

        public void RemoveClient(int clientId)
        {
            if (!clientQueues.TryRemove(clientId, out var clientQueue))
            {
                Logger.LogError($"Client with ID {clientId} does not exist.");
                return;
            }

            clientQueue.Handler?.Disconnect();
        }

        public void EnqueueReceive(int clientId, int packetType, byte[] payload)
        {
            if (clientQueues.TryGetValue(clientId, out var clientQueue))
            {
                clientQueue.ReceiveQueue.Enqueue(new Message { PacketType = packetType, Payload = payload });
                // 클라이언트 데이터 순차적으로 받는곳
            }
            else
            {
                Logger.LogError($"Client with ID {clientId} not found.");
            }
        }

        public void EnqueueSend(int clientId, byte[] data)
        {
            if (clientQueues.TryGetValue(clientId, out var clientQueue))
            {
                clientQueue.SendQueue.Enqueue(data);
                // 클라이언트에 데이터 순차적으로 보내주는 곳
            }
            else
            {
                Logger.LogError($"Client with ID {clientId} not found.");
            }
        }

        private async void ProcessReceiveQueue(int clientId)
        {
            if (!clientQueues.TryGetValue(clientId, out var clientQueue)) return;

            if (clientQueue.isProcessingReceive) return;
            clientQueue.isProcessingReceive = true;

            while(clientQueue.ReceiveQueue.TryDequeue(out var message))
            {
                try
                {
                    //
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing message for client {clientId}: {ex.Message}");
                }
            }

            clientQueue.isProcessingReceive = false;
        }

        private async void ProcessSendQueue(int clientId)
        {
            if (!clientQueues.TryGetValue(clientId, out var clientQueue)) return;

            if (clientQueue.isProcessingSend) return;
            clientQueue.isProcessingSend = true;

            while (clientQueue.SendQueue.TryDequeue(out var data))
            {
               try
                {
                    //
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error sending message to client {clientId}: {ex.Message}");
                }
            }

            clientQueue.isProcessingSend = false;
        }
    }
}
