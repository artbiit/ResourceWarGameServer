using Cysharp.Threading.Tasks;
using Protocol;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server {
    
public partial class MessageHandlers : Singleton<MessageHandlers>
{
    private async UniTask<Packet> SignInHandler(Packet packet)
    {
            Logger.Log(packet.Payload.ToString());
        var pongMessage = (C2SSignInReq)packet.Payload;
        Packet result = new Packet();
        S2CSignInRes payload = new S2CSignInRes
        {
            ExpirationTime = 0,
            SignInResultCode = 1,
            Token = "테스트용 응답 값"
        };
        result.Payload = payload;
        result.Token = "";
        result.PacketType = PacketType.SIGN_IN_RESPONSE;
        return result;
    }
}
}