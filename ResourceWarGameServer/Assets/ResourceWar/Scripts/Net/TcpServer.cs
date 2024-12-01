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

namespace ResourceWar.Server
{
    public class TcpServer : MonoSingleton<TcpServer>
    {
        private TcpListener tcpListener; // TCP 연결을 대기하는 리스너
        private int clientIdCounter = 0; // 클라이언트 ID를 고유하게 생성하기 위한 카운터
        private ConcurrentDictionary<int, ClientHandler> clients = new(); // 연결된 클라이언트의 관리

        // 서버 초기화
        public void Init(string bind, int port)
        {
            if (tcpListener == null)
            {
                tcpListener = new TcpListener(IPAddress.Parse(bind), port); // 지정된 IP와 포트로 리스너 생성
                TcpServer.Instance.Listen();
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
            if (tcpListener.Server.IsBound)
            {
                Logger.LogWarning("TCP Listener is already started.");
                return;
            }
            tcpListener.Start(); // TCP 리스너 시작
            Logger.Log($"서버 시작함"); // 초기화 로그 출력
            _ = AcceptClientAsync(); // 비동기 클라이언트 수락 시전
        }

        // 클라이언트 연결 수락
        private async UniTaskVoid AcceptClientAsync()
        {
            while (tcpListener != null)
            {
   
                TcpClient client = await tcpListener.AcceptTcpClientAsync();  // 새 클라이언트 연결 대기
                int clientId = clientIdCounter++; // 고유 클라이언트 ID 생성
                // 클라이언트 처리 핸들러 생성
                Logger.Log($"New Client : {clientId}");
                var clientHandler = new ClientHandler(clientId, client, this.RemoveClient);
                this.clients.TryAdd(clientId, clientHandler); // 클라이언트 목록에 추가
                clientHandler.StartHandling(); // 클라이언트 데이터 처리 시작
            }
        }

        public bool TryGetClient(int clientId, out ClientHandler clientHandler) => this.clients.TryGetValue(clientId, out clientHandler);
        

        // 클라이언트 연결 제거
        private async void RemoveClient(int clientId)
        {
            clients.TryRemove(clientId, out _); // 클라이언트 목록에서 제거
            await EventDispatcher<GameManager.GameManagerEvent, int>.Instance.NotifyAsync(GameManager.GameManagerEvent.ClientRemove, clientId);
        }

        // 서버 정지
        public void StopServer()
        {
            if (tcpListener != null)
            {
                tcpListener.Stop(); // TCP 리스너 정지
                tcpListener = null;
            }
        }

        protected override void OnDestroy()
        {
            StopServer();
            base.OnDestroy();
        }
    }
}
