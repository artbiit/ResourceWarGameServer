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
        EventDispatcher<PacketType, Packet>.Instance.Subscribe(PacketType.PONG_RESPONSE, PongHandling);
        _ = Pong();
    }


    private async UniTask PongHandling(Packet packet)
    {
        Logger.Log($"PongHandling => {packet.Payload.ToString()}");
    }

    private async UniTask Pong()
    {
        Logger.Log("Pong Start");
        await EventDispatcher<PacketType, Packet>.Instance.NotifyAsync(PacketType.PONG_RESPONSE, new Packet {

            PacketType = PacketType.PONG_RESPONSE,
            Payload = new C2SPongRes { ClientTime = DateTime.UtcNow.Ticks },
            Token = ""
        });

        Logger.Log("Pong End");
        
    }
}
