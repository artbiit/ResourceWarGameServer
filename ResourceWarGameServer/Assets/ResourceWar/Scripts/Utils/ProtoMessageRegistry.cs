using Google.Protobuf;
using System.Collections.Concurrent;
using System.Diagnostics;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    /// <summary>
    /// Protobuf 메시지와 패킷 타입 간 매핑을 관리하는 레지스트리 클래스
    /// 패킷 타입에 따라 메시지를 등록하고 가져올 수 있습니다.
    /// </summary>
    public static class ProtoMessageRegistry
    {
        // 패킷 타입과 메시지 템플릿 간 매핑을 저장하는 dictionary
        private static readonly ConcurrentDictionary<ushort, IMessage> Messages = new();

        /// <summary>
        /// 특정 패킷 타입에 Protobuf 메시지 템플릿을 등록합니다.
        /// </summary>
        /// <typeparam name="T">등록할 메시지의 타입</typeparam>
        /// <param name="packetType">패킷 타입 (ushort)</param>
        /// <param name="message">등록할 Protobuf 메시지 객체</param>
        public static void RegisterMessage<T>(ushort packetType, T message) where T : IMessage
        {
            Messages[packetType] = message;
            Logger.Log($"테스트 중입니다. => ${message}");
        }

        /// <summary>
        /// 패킷 타입에 해당하는 Protobuf 메시지 템플릿을 반환합니다.
        /// </summary>
        /// <param name="packetType">검색할 패킷 타입</param>
        /// <returns>Protobuf 메시지 템플릿 (IMessage) 또는 null</returns>
        public static IMessage GetMessage(ushort packetType)
        {
            return Messages.TryGetValue(packetType, out var message) ? message : null;
        }
    }
}
