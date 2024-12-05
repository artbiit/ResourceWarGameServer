using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public partial class MessageHandlers : Singleton<MessageHandlers>
    {
        private async UniTask<Packet> FurnaceHandler(ReceivedPacket packet)
        {
            await EventDispatcher<Furnace.Event, ReceivedPacket>
                .Instance.NotifyAsync(Furnace.Event.FurnaceRequest, packet);

            return null;
        }
    }
}
