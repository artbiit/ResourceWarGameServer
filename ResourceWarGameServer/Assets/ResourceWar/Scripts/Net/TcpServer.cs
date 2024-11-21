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
                Logger.Log("TcpServer initialized"); // 초기화 로그 출력
            }
            else
            {
                Logger.LogError("TcpServer is Already Initialized"); // 이미 초기화된 경우 에러 로그 출력
            }
        }

        // 클라이언트 연결 대기 시작
        public void Listen()
        {
            tcpListener.Start(); // TCP 리스너 시작
            _ = AcceptClientAsync(); // 비동기 클라이언트 수락 시전
        }

        // 클라이언트 연결 수락
        private async UniTaskVoid AcceptClientAsync()
        {
            while (true)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();  // 새 클라이언트 연결 대기
                int clientId = Interlocked.Increment(ref clientIdCounter); // 고유 클라이언트 ID 생성

                // 클라이언트 처리 핸들러 생성
                var clientHandler = new ClientHandler(clientId, client, this.RemoveClient);
                this.clients.TryAdd(clientId, clientHandler); // 클라이언트 목록에 추가
                clientHandler.StartHandling(); // 클라이언트 데이터 처리 시작
            }
        }

        // 클라이언트 연결 제거
        private void RemoveClient(int clientId)
        {
            clients.TryRemove(clientId, out _); // 클라이언트 목록에서 제거
        }

        // 서버 정지
        public void StopServer()
        {
            if (tcpListener != null)
            {
                tcpListener.Stop(); // TCP 리스너 정지
            }
        }
    }
}
