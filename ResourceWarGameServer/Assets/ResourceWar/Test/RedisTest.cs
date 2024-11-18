using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;

namespace ResourceWar.Server { 
public class RedisTest : MonoBehaviour
{
    
     RedisClient redisClient;
    // Start is called before the first frame update
    void Start()
    {
            redisClient = new RedisClient();
            redisClient.Connect("positivenerd.duckdns.org", 15005);
            redisClient.SetKey("테스트임ㅎㅎ", "테스트 값");
            Debug.Log(redisClient.GetKey("테스트임ㅎㅎ"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
}