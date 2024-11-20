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
        private readonly ClientHandler handler;
        private readonly ConcurrentQueue<Message> receiveQueue = new();
        private readonly ConcurrentQueue<byte[]> sendQueue = new();
        private bool isProcessingReceive = false;
        private bool isProcessingSend = false;

        public struct Message
        {
            public int PacketType;
            public byte[] Payload;
        }

        public MessageQueue(ClientHandler handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void EnqueuReceive(int packetType, byte[] payload)
        {
            receiveQueue.Enqueue(new Message { PacketType = packetType, Payload = payload });
            ProcessReceiveQueue();
        }

        public async void EnqueueSend(byte[] data)
        {
            sendQueue.Enqueue(data);
            ProcessSendQueue();
        }

        private async void ProcessReceiveQueue()
        {
            if (isProcessingReceive) return;
            isProcessingReceive = true;

            while (receiveQueue.TryDequeue(out var message))
            {
                try
                {
                    await handler.HandleMessage(message.PacketType, message.Payload);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error processing message for client {handler.ClientId}: {ex.Message}");
                }
            }

            isProcessingReceive = false;
        }

        private async void ProcessSendQueue()
        {
            if (isProcessingSend) return;
            isProcessingSend = true;

            while (sendQueue.TryDequeue(out var data))
            {
                try
                {
                    await handler.SendToClient(data);
                }
                catch(Exception ex)
                {
                    Logger.LogError($"Error sending message to client {handler.ClientId}");
                }
            }

            isProcessingSend = false;
        }
    }
}
