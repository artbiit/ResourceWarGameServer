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
            DotEnv.Config();

            redisClient = RedisClient.Instance;
            redisClient.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_REDIS_PASSWORD"));
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