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
        public DateTime Timestamp { get; set; } // 패킷 생성 또는 송수신 시점

        /// <summary>
        /// 스트림에서 패킷 읽기
        /// </summary>
        public static Packet FromStream(BinaryReader reader)
        {
            // 데이터 읽기 위한 BinaryReader
            try
            {
                PacketType packetType = (PacketType)PacketUtils.ReadUInt16BigEndian(reader); // 패킷 타입 읽기
                int tokenLength = reader.ReadByte(); // 토큰 길이 읽기
                string token = Encoding.UTF8.GetString(reader.ReadBytes(tokenLength)); // 토큰 데이터 읽기
                int payloadLength = PacketUtils.ReadInt32BigEndian(reader); // 페이로드 길이 읽기
                byte[] payloadBytes = reader.ReadBytes(payloadLength);

                var protoMessages = PacketUtils.CreateMessage(packetType); // 패킷 타입에 맞는 Protobuf 메시지 검색
                IMessage payload = protoMessages.Descriptor.Parser.ParseFrom(payloadBytes); // 페이로드 파싱

                return new Packet { PacketType = packetType, Token = token, Payload = payload };
            }
            catch(Exception e) 
            {
                Logger.LogError(e);
                return null; // 데이터가 부족하거나 잘못된 경우 null 반환 (반드시 형식을 맞춰 보내야함)
            }
        }

        /// <summary>
        /// 패킷 데이터를 바이트 배열로 반환
        /// </summary>

        public byte[] ToBytes()
        {
            var tokenBytes = Encoding.UTF8.GetBytes(Token); // 토큰 데이터를 바이트로 변환
            var payloadBytes = Payload.ToByteArray();

            using var stream = new MemoryStream(); // 메모리 스트림 생성
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true); // BinaryWriter로 데이터를 쓰기
            writer.Write((ushort)PacketType); // 패킷 타입 쓰기
            writer.Write((byte)tokenBytes.Length); // 토큰 길이 쓰기
            writer.Write(tokenBytes); // 토큰 데이터 쓰기
            writer.Write(payloadBytes.Length); // 페이로드 길이 쓰기
            writer.Write(payloadBytes); // 페이로드 데이터 쓰기

            return stream.ToArray(); // 스트림 내용을 바이트 배열로 반환
        }
    }
}
