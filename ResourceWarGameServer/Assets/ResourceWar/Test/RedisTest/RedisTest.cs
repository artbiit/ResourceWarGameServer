using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;

namespace ResourceWar.Server { 
public class RedisTest : MonoBehaviour
{
    
     RedisClient redisClient;
    // Start is called before the first frame update
    void Start()
    {
            _ = test();
    }

        private async UniTask test()
        {
            redisClient = RedisClient.Instance;
            redisClient.Connect("positivenerd.duckdns.org", 15005);
            await  redisClient.ExecuteAsync(db =>  db.StringSetAsync("테스트임ㅎㅎ", "테스트 값222"));
            var result = await redisClient.ExecuteAsync(db => db.StringGetAsync("테스트임ㅎㅎ"));
            Debug.Log(result);
        }

    // Update is called once per frame
    void Update()
    {
        
    }
}
}