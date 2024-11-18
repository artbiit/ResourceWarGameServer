using System;
using Account; // account.proto에서 생성된 네임스페이스
using Google.Protobuf; // Google.Protobuf 라이브러리 사용

namespace ResourceWar.Server.Assets.ResourceWar.Scripts.ProtobufTest
{
    internal class Test
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Protobuf Test 시작 ===");

            // 1. 회원가입 요청 메시지 테스트
            var signUpReq = new C2SSignUpReq
            {
                Nickname = "TestUser",
                Id = "testuser123",
                Password = "password123"
            };

            // 직렬화
            byte[] signUpSerialized = signUpReq.ToByteArray();
            Console.WriteLine($"Serialized SignUpReq: {BitConverter.ToString(signUpSerialized)}");

            // 역직렬화
            var deserializedSignUpReq = C2SSignUpReq.Parser.ParseFrom(signUpSerialized);
            Console.WriteLine($"Deserialized SignUpReq: Nickname={deserializedSignUpReq.Nickname}, Id={deserializedSignUpReq.Id}, Password={deserializedSignUpReq.Password}");

            // 2. 로그인 요청 메시지 테스트
            var signInReq = new C2SSignInReq
            {
                Id = "testuser123",
                Password = "password123"
            };

            // 직렬화
            byte[] signInSerialized = signInReq.ToByteArray();
            Console.WriteLine($"Serialized SignInReq: {BitConverter.ToString(signInSerialized)}");

            // 역직렬화
            var deserializedSignInReq = C2SSignInReq.Parser.ParseFrom(signInSerialized);
            Console.WriteLine($"Deserialized SignInReq: Id={deserializedSignInReq.Id}, Password={deserializedSignInReq.Password}");

            // 3. 토큰 재발급 요청 메시지 테스트
            var refreshTokenReq = new C2SRefreshTokenReq
            {
                Token = "sample_refresh_token"
            };

            // 직렬화
            byte[] refreshTokenSerialized = refreshTokenReq.ToByteArray();
            Console.WriteLine($"Serialized RefreshTokenReq: {BitConverter.ToString(refreshTokenSerialized)}");

            // 역직렬화
            var deserializedRefreshTokenReq = C2SRefreshTokenReq.Parser.ParseFrom(refreshTokenSerialized);
            Console.WriteLine($"Deserialized RefreshTokenReq: Token={deserializedRefreshTokenReq.Token}");

            Console.WriteLine("=== Protobuf Test 종료 ===");
        }
    }
}
