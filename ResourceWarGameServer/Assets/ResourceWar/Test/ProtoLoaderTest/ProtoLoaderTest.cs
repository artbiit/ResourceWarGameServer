using UnityEngine;

namespace ResourceWar.Server
{
    /// <summary>
    /// ProtoLoader를 테스트하기 위한 Unity 컴포넌트
    /// </summary>
    public class ProtoLoaderTester : MonoBehaviour
    {
        // Start는 Unity에서 GameObject 실행 시 호출됩니다.
        void Start()
        {
            Debug.Log("[ProtoLoaderTester] Starting Protobuf loading test...");

            // Protobuf 메시지 로드
            ProtoLoader.LoadProtos();

            Debug.Log("[ProtoLoaderTester] Protobuf loading test completed.");
        }
    }
}
