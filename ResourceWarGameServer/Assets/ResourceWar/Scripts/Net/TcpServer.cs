using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using System.Net.Sockets;
using System.Net;
using Logger = ResourceWar.Server.Lib.Logger;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System;
using Codice.Client.Common.GameUI;
using UnityEditor.PackageManager;
namespace ResourceWar.Server
{
    public class TcpServer : MonoSingleton<TcpServer>
    {
        private TcpListener tcpListener;
        private int clientIdCounter = 0;
        private ConcurrentDictionary<int, ClientHandler> clients = new();
        public void Init(string bind, int port)
        {
            if (tcpListener == null)
            {
                tcpListener = new TcpListener(IPAddress.Parse(bind), port);
                Logger.Log("TcpServer initialized");
            }
            else
            {
                Logger.LogError("TcpServer is Already Initialized");
            }
        }

        public void Listen()
        {
            tcpListener.Start();
            _ = AcceptClientAsync();
        }

        private async UniTaskVoid AcceptClientAsync()
        {
            while (true)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                int clientId = Interlocked.Increment(ref clientIdCounter);

                var clientHandler = new ClientHandler(clientId, client, this.RemoveClient);
                this.clients.TryAdd(clientId, clientHandler);
                clientHandler.StartHandling();
            }
        }

        private void RemoveClient(int clientId)
        {
            clients.TryRemove(clientId, out _);
        }

        public void StopServer()
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }
    }

    class ClientHandler
    {
        private readonly int clientId;
        private readonly TcpClient tcpClient;
        private readonly NetworkStream stream;
        private readonly ConcurrentQueue<string> sendQueue = new();
        private readonly ConcurrentQueue<string> receiveQueue = new();
        private readonly Action<int> onDisconnect;

        private CancellationTokenSource cts = new();

        public ClientHandler(int clientId, TcpClient tcpClient, Action<int> onDisconnect)
        {
            this.clientId = clientId;
            this.tcpClient = tcpClient;
            this.onDisconnect = onDisconnect;
            this.stream = this.tcpClient.GetStream();
        }

        public void StartHandling()
        {
            _ = HandleReceivingAsync();
            _ = HandleSendingAsync();
        }

        private async UniTaskVoid HandleReceivingAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    int byteRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

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

        private async UniTaskVoid HandleSendingAsync()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    while (sendQueue.TryDequeue(out var message))
                    {
                        //여기다가 전송 넣어야함
                        //await stream.WriteAsync()
                    }

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


        private void Disconnect()
        {
            cts?.Cancel();
            stream?.Close();
            tcpClient?.Close();
            onDisconnect?.Invoke(clientId);
        }
    }
}
