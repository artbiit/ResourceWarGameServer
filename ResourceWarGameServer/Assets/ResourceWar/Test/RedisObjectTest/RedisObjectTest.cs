using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using ResourceWar.Server.Lib;
using Cysharp.Threading.Tasks;
using Logger = ResourceWar.Server.Lib.Logger;
using Newtonsoft.Json;
public class RedisObjectTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        RedisTest().Forget();
    }

   public async UniTask RedisTest()
    {
        DotEnv.Config();
        await RedisClient.Instance.ConnectAsync(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
        GameSessionInfo gameSessionInfo = new GameSessionInfo();
        gameSessionInfo.currentPlayer = 20;
         await RedisClient.Instance.SaveObjectToHash("MyTest", gameSessionInfo);

        GameSessionInfo loadSessionInfo = await RedisClient.Instance.LoadObjectFromHash<GameSessionInfo>("MyTest");
       
        Logger.Log(JsonConvert.SerializeObject(gameSessionInfo));
        Logger.Log(JsonConvert.SerializeObject(loadSessionInfo));

    }
}
