using Cysharp.Threading.Tasks;
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
        private async UniTask<Packet> GameStartHandler(ReceivedPacket packet)
        {
            var result = new Packet
            {
                PacketType = PacketType.GAME_START_RESPONSE
            };

            

            return null;
        }
    }
}
