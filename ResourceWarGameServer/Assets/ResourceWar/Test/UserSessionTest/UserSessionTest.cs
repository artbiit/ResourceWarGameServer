using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using ResourceWar.Server.Lib;

public class UserSessionTest : MonoBehaviour
{
    public string UserToken = "7b5e6aa1-87f2-4174-8996-f827012d2e8a";
    // Start is called before the first frame update
    void Start()
    {
        DotEnv.Config();
        RedisClient.Instance.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
        _ = Test();
    }

    private async UniTask Test()
    {

       var userInfo = await UserRedis.GetUserSession(UserToken);

        Debug.Log(userInfo.Count);

        foreach (var item in userInfo)
        {
            Debug.Log($"{item.Key} - {item.Value}");
        }

        UserSession userSession = new UserSession(userInfo);
        Debug.Log(userSession.ID);
        Debug.Log(userSession.ExpirationTime);
        Debug.Log(UnixTime.ToDateTime(userSession.ExpirationTime));

    }
}
