using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        public async UniTask<Packet> AddMonsterRequestHandler(ReceivedPacket receivedPacket)
        {

           // var payload = (Protocol.C2SMonsterAddReq)receivedPacket.Payload;
            await EventDispatcher<MonsterController.Event, ReceivedPacket>.Instance.NotifyAsync(MonsterController.Event.AddNewTeam, receivedPacket);
            return null;
        }
    }
}
