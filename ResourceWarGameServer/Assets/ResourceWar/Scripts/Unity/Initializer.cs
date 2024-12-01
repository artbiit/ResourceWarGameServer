using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server.Lib;
using Logger = ResourceWar.Server.Lib.Logger;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using Cysharp.Threading.Tasks.Linq;


namespace ResourceWar.Server
{
    public class Initializer : MonoBehaviour
    {

        private void Awake()
        {
            Init().Forget();
        }

        private async UniTask Init()
        {
            string gameCode = GameManager.GenerateGameCode();
            Logger.Log("-------------Initializer-------------");
            await GameRedis.SetGameState(gameCode,GameSessionState.CREATING);
            Logger.Log("Start DataLayer");
            DotEnv.Config();
            var (postgresqlResult, redisResult) = await UniTask.WhenAll(TryInitialize(PostogresqlInit), TryInitialize(RedisInit));
            if(postgresqlResult && redisResult)
            {
                Logger.Log("-------------DataLayer Initialized-------------");
                TcpServer.Instance.Init(DotEnv.Get<string>("SERVER_BIND"), DotEnv.Get<int>("SERVER_PORT"));
                Logger.Log("-------------Initializer-------------");
                SceneManager.LoadScene("Game");
            }
            else
            {
                Logger.LogError("-------------DataLayer Initialize Failed-------------");
                await GameRedis.SetGameState(gameCode, GameSessionState.ERROR);
#if !UNITY_EDITOR
                Application.Quit(1);
#endif
            }
            
            
           
        }

        private async UniTask<bool> PostogresqlInit() => await PostgreSQLClient.Instance.ConnectAsync(DotEnv.Get<string>("DB_HOST"), DotEnv.Get<int>("DB_PORT"), DotEnv.Get<string>("DB_NAME"), DotEnv.Get<string>("DB_USER"), DotEnv.Get<string>("DB_PASSWORD"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MIN"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MAX"));
        private async UniTask<bool> RedisInit() => await RedisClient.Instance.ConnectAsync(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));


        private async UniTask<bool> TryInitialize(Func<UniTask<bool>> tryFunction, int tryCount = 5)
        {
            bool result = false;
            for (int i = 0; i < tryCount; i++)
            {
                try
                {
                    result = await tryFunction();
                    if (result)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }

                if (i < tryCount - 1) 
                {
                    await UniTask.Delay((i + 1) * 1000);
                }
            }

            return result;
        }
    }
}
