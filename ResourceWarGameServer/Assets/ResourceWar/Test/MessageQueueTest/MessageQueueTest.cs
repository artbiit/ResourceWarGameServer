using System.Text;
using UnityEngine;
using ResourceWar.Server;
using ResourceWar.Utils;

public class MessageQueueTest : MonoBehaviour
{
    private ClientHandler clientHandler;
    private MessageQueue messageQueue;

    void Start()
    {
        // 실제 연결 테스트
        var tcpClient = new System.Net.Sockets.TcpClient();
        tcpClient.Connect("127.0.0.1", 7777);

        clientHandler = new ClientHandler(1, tcpClient, OnClientDisconnected);
        messageQueue = new MessageQueue(clientHandler);

        TestReceiveQueue();
        TestSendQueue();
    }


    private void TestReceiveQueue()
    {
        Debug.Log("Testing Receive Queue...");

        // 수신 큐에 데이터 추가
        messageQueue.EnqueuReceive(1, Encoding.UTF8.GetBytes("Test Receive Payload 1"));
        messageQueue.EnqueuReceive(2, Encoding.UTF8.GetBytes("Test Receive Payload 2"));
    }

    private void TestSendQueue()
    {
        Debug.Log("Testing Send Queue...");

        // 송신 큐에 데이터 추가
        messageQueue.EnqueueSend(Encoding.UTF8.GetBytes("Test Send Payload 1"));
        messageQueue.EnqueueSend(Encoding.UTF8.GetBytes("Test Send Payload 2"));
    }

    private void OnClientDisconnected(int clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
    }

    void Update()
    {
        // 지속적인 처리 확인 (선택적으로 추가)
    }
}