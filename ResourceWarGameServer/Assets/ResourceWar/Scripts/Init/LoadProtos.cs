using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Protocol;
using UnityEditor.VersionControl;
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

            var mappings = new Dictionary<string, IMessage>();
            foreach (var messageDescriptor in ProtocolReflection.Descriptor.MessageTypes)
            {


                IMessage messageInstance = messageDescriptor.Parser.ParseFrom(new byte[0]);
                mappings.Add(messageDescriptor.FullName, messageInstance);
                Debug.Log($"Registered Protobuf message: {messageDescriptor.FullName} with Fields : {string.Join(',', messageDescriptor.Fields.InDeclarationOrder().Select(s => s.Name))}");
                // Enum 값에서 패킷 타입을 검색
            }
        }

    }
}
