using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ResourceWar.Server;
using Google.Protobuf;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using Protocol;
using System;

public class EventDispatcherTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //디스팻처에 콜백 등록
        EventDispatcher<PacketType, Packet>.Instance.Subscribe(PacketType.PLAYER_MOVE, PongHandling);
        _ = Pong();
    }


    //콜백에 등록된 메소드
    private async UniTask PongHandling(Packet packet)
    {
        Logger.Log($"PongHandling => {packet.Payload.ToString()}");
    }

    //디스팻처 이벤트 알림
    private async UniTask Pong()
    {
        Logger.Log("Pong Start");
        await EventDispatcher<PacketType, Packet>.Instance.NotifyAsync(PacketType.PLAYER_MOVE, new Packet {

            PacketType = PacketType.PONG_RESPONSE,
            Payload = new C2SPongRes { ClientTime = DateTime.UtcNow.Ticks },
            Token = ""
        });

        Logger.Log("Pong End");
        
    }
}
