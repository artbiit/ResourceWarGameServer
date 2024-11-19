using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class Initializer : MonoBehaviour
    {

        private void Awake()
        {
            Logger.Log("-------------Initializer-------------");
            Logger.Log("Start DataLayer");
            DotEnv.Config();
            PostgreSQLClient.Instance.Connect(DotEnv.Get<string>("DB_HOST"), DotEnv.Get<int>("DB_PORT"), DotEnv.Get<string>("DB_NAME"), DotEnv.Get<string>("DB_USER"), DotEnv.Get<string>("DB_PASSWORD"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MIN"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MAX"));
            RedisClient.Instance.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
            Logger.Log("-------------DataLayer Initialized-------------");
            TcpServer.Instance.Init(DotEnv.Get<string>("SERVER_BIND"), DotEnv.Get<int>("SERVER_PORT"));
            Logger.Log("-------------Initializer-------------");
        }
       
    }
}
