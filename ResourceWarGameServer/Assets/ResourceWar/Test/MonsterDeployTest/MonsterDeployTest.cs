using ResourceWar.Server;
using ResourceWar.Server.Lib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public class MonsterDeployTest : MonoBehaviour
{
    public MonsterController monsterController;
    public int spawnCount = 4;
    private void Awake()
    {
        DotEnv.Config();
        TableData.Load();
        RedisClient.Instance.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
        PostgreSQLClient.Instance.Connect(DotEnv.Get<string>("DB_HOST"), DotEnv.Get<int>("DB_PORT"), DotEnv.Get<string>("DB_NAME"), DotEnv.Get<string>("DB_USER"), DotEnv.Get<string>("DB_PASSWORD"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MIN"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MAX"));

       
        var keys = TableData.Monsters.Keys.ToArray();
        int[] spawnMonster = new int[spawnCount * keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            for (var j = 0; j < spawnCount; j++)
            {
                spawnMonster[i * (keys.Length-1) + j] = keys[i];
            }
        }


        monsterController.AddMonster(1, spawnMonster);
        monsterController.AddMonster(2, spawnMonster);

    }
}
