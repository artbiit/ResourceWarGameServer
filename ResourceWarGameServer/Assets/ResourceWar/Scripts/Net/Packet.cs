using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Google.Protobuf;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
   

    public class Packet 
    {
        public PacketType PacketType { get; set; }
        public string Token { get; set; }
        public IMessage Payload { get; set; } // Protobuf 메시지

        /// <summary>
        /// 패킷 데이터를 바이트 배열로 반환
        /// </summary>
        public byte[] ToBytes()
        {
            Token = Token ?? "";
            var tokenBytes = Encoding.UTF8.GetBytes(Token); // 토큰 데이터를 바이트로 변환
            var payloadBytes = Payload.ToByteArray(); // Protobuf 페이로드를 바이트 배열로 변환

            using var stream = new MemoryStream(); // 메모리 스트림 생성
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true); // BinaryWriter로 데이터를 쓰기

            // 패킷 타입 (ushort -> 빅 엔디안)
            byte[] packetTypeBytes = BitConverter.GetBytes((ushort)PacketType);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(packetTypeBytes); // 리틀 엔디안 -> 빅 엔디안 변환
            }
            writer.Write(packetTypeBytes);

            // 토큰 길이 (byte)
            writer.Write((byte)tokenBytes.Length);

            // 토큰 데이터
            writer.Write(tokenBytes);

            // 페이로드 길이 (int -> 빅 엔디안)
            byte[] payloadLengthBytes = BitConverter.GetBytes(payloadBytes.Length);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(payloadLengthBytes); // 리틀 엔디안 -> 빅 엔디안 변환
            }
            writer.Write(payloadLengthBytes);

            // 페이로드 데이터
            writer.Write(payloadBytes);

            return stream.ToArray(); // 스트림 내용을 바이트 배열로 반환
        }
    }

    /// <summary>
    /// 수신시 생성할 패킷 클래스
    /// </summary>
    public class ReceivedPacket : Packet
    {
        public readonly int ClientId;
        public ReceivedPacket(int clientId) : base()
        {
            this.ClientId = clientId;
        }
    }
}
