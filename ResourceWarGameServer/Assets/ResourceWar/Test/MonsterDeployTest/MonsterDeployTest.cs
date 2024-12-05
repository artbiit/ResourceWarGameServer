using ResourceWar.Server;
using ResourceWar.Server.Lib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterDeployTest : MonoBehaviour
{
    public MonsterController monsterController;

    private void Awake()
    {
        DotEnv.Config();
        TableData.Load();

        foreach (var item in TableData.Items)
        {
            Debug.Log($"{item.Key} - {item.Value.ItemType}");
        }

        return;
        RedisClient.Instance.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
        PostgreSQLClient.Instance.Connect(DotEnv.Get<string>("DB_HOST"), DotEnv.Get<int>("DB_PORT"), DotEnv.Get<string>("DB_NAME"), DotEnv.Get<string>("DB_USER"), DotEnv.Get<string>("DB_PASSWORD"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MIN"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MAX"));

        foreach (var item in TableData.Monsters)
        {
            Debug.Log($"{item.Key} - {item.Value.Name} / {item.Value.Position}");
        }
    }
}
