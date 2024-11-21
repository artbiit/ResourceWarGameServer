using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Protocol;
using UnityEngine;

namespace ResourceWar.Server
{
    public static class ProtoLoader
    {
        /// <summary>
        /// Protocol.cs의 모든 Protobuf 메시지를 ProtoMessageRegistry에 등록
        /// </summary>
        public static void LoadProtos()
        {
            // ProtocolReflection.Descriptor에서 메시지 디스크립터와 Enum 값을 가져옵니다.
            var enumMappings = BuildEnumMappings();

            foreach (var message in ProtocolReflection.Descriptor.MessageTypes)
            {
                var messageType = message.ClrType;

                // Enum 값에서 패킷 타입을 검색
                if (enumMappings.TryGetValue(message.Name, out var packetType))
                {
                    ProtoMessageRegistry.RegisterMessage((ushort)packetType, (IMessage)Activator.CreateInstance(messageType));
                    Debug.Log($"Registered Protobuf message: {message.Name} with PacketType: {packetType}");
                }
                else
                {
                    Debug.LogWarning($"Skipping unregistered Protobuf message: {message.Name}");
                }
            }
        }

        /// <summary>
        /// ProtocolReflection에서 Enum 타입과 값을 매핑합니다.
        /// </summary>
        /// <returns>Enum 이름과 숫자 값의 매핑 딕셔너리</returns>
        private static Dictionary<string, int> BuildEnumMappings()
        {
            var mappings = new Dictionary<string, int>();

            foreach (var enumType in ProtocolReflection.Descriptor.EnumTypes)
            {
                foreach (var value in enumType.Values)
                {
                    mappings[value.Name] = value.Number;
                }
            }

            return mappings;
        }
    }
}
