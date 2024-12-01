using ResourceWar.Server.Lib;
using System.Net.Sockets;
using System.Net;
using Logger = ResourceWar.Server.Lib.Logger;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Assertions;
using System.Linq;

namespace ResourceWar.Server
{
    public class TcpServer : MonoSingleton<TcpServer>
    {
        private TcpListener tcpListener; // TCP 연결을 대기하는 리스너
        private int clientIdCounter = 0; // 클라이언트 ID를 고유하게 생성하기 위한 카운터
        private ConcurrentDictionary<int, ClientHandler> clients = new(); // 연결된 클라이언트의 관리
        private int previousPlayerCount = 0; //연결했으나 인증 대기중인 클라이언트 수
        private CancellationTokenSource cts; // 연결 대기 제어를 위한 CancellationTokenSource
        // 서버 초기화
        public void Init(string bind, int port)
        {
            if (tcpListener == null)
            {
                EventDispatcher<GameManager.GameManagerEvent, ReceivedPacket>.Instance.Subscribe(GameManager.GameManagerEvent.AddNewPlayer, ClientAuthroizedEvent);
                tcpListener = new TcpListener(IPAddress.Parse(bind), port); // 지정된 IP와 포트로 리스너 생성
                Listen();
                Logger.Log($"TcpServer initialized [{tcpListener.LocalEndpoint}]"); // 초기화 로그 출력
            }
            else
            {
                Logger.LogError("TcpServer is Already Initialized"); // 이미 초기화된 경우 에러 로그 출력
            }
        }

        // 클라이언트 연결 대기 시작
        public void Listen()
        {
            if (cts != null)
            {
                Logger.LogWarning("TCP Listener is already started.");
                return;
            }
            cts = new CancellationTokenSource();
            tcpListener.Start(); // TCP 리스너 시작
            Logger.Log($"서버 시작함"); // 초기화 로그 출력
            _ = AcceptClientAsync(cts.Token); // 비동기 클라이언트 수락 시전
        }

        // 클라이언트 연결 수락
        private async UniTaskVoid AcceptClientAsync(CancellationToken cancellationToken)
        {
            try { 
            while (!cancellationToken.IsCancellationRequested)
            {

                TcpClient client = await tcpListener.AcceptTcpClientAsync();  // 새 클라이언트 연결 대기
                if (cancellationToken.IsCancellationRequested)
                {
                    client?.Close();
                    break;
                }
                int clientId = clientIdCounter++; // 고유 클라이언트 ID 생성
               
                // 클라이언트 처리 핸들러 생성
                Logger.Log($"New Client : {clientId}");
                var clientHandler = new ClientHandler(clientId, client, this.RemoveClient);
                this.clients.TryAdd(clientId, clientHandler); // 클라이언트 목록에 추가
                clientHandler.StartHandling(); // 클라이언트 데이터 처리 시작
                await ChangePreviousPlayer(true);
            }
            }
            catch (ObjectDisposedException)
            {
                Logger.Log("TcpListener has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in AcceptClientAsync: {ex.Message}");
            }
        }

        public bool TryGetClient(int clientId, out ClientHandler clientHandler) => this.clients.TryGetValue(clientId, out clientHandler);
        
        /// <summary>
        /// 어떤 클라이언트든 인증에 완료하였을 때
        /// </summary>
        /// <param name="receivedPacket"></param>
        /// <returns></returns>
        private async UniTask ClientAuthroizedEvent(ReceivedPacket receivedPacket)
        {
            await  ChangePreviousPlayer(false);
            
            if(clients.Count(client => client.Value.IsAuthorized) >= 4)
            {
                StopListen(); //더이상 새 클라이언트를 받지 않음
                int playerCount = 0;
                foreach (var client in clients.Values)
                {
                    if (client.IsAuthorized == false || playerCount >= 4) //이미 가득찼으면 인증했어도 추방
                    {
                        client.Disconnect();
                        continue;
                    }
                    playerCount++;
                }
            }
            await UniTask.CompletedTask;
        }

        public async UniTask ChangePreviousPlayer(bool isIncrease)
        {
            if(String.IsNullOrWhiteSpace( GameManager.GameCode) || tcpListener == null)
            {
                Logger.LogError($"GameServer[{GameManager.GameCode}] is not listening");
                return;
            }
            if (isIncrease) { 
                 previousPlayerCount = Interlocked.Increment(ref previousPlayerCount);
            }
            else
            {
                previousPlayerCount = Interlocked.Decrement(ref previousPlayerCount);
            }
            await  GameRedis.SetPreviousPlayerCount(GameManager.GameCode, previousPlayerCount);
        }

        // 클라이언트 연결 제거
        private async void RemoveClient(int clientId)
        {
            clients.TryRemove(clientId, out _); // 클라이언트 목록에서 제거
            if(clients.Count <= 4 && cts == null) //제거된 후 연결대기중이 아니면 대기상태로 전환
            {
                Listen();
            }
            await EventDispatcher<GameManager.GameManagerEvent, int>.Instance.NotifyAsync(GameManager.GameManagerEvent.ClientRemove, clientId);
        }

        public void StopListen()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }

        // 서버 정지
        public void StopServer()
        {
            if (tcpListener != null)
            {
                cts?.Cancel();
                tcpListener.Stop(); // TCP 리스너 정지
                tcpListener = null;
                cts?.Dispose();
                cts = null;
                Logger.Log("Server stopped.");
            }
        }

        protected override void OnDestroy()
        {
            StopServer();
            base.OnDestroy();
        }
    }
}
