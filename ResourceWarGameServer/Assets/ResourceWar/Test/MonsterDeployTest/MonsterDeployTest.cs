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
    public int[] spawnCount = new int[4];
    private void Awake()
    {
        DotEnv.Config();
        TableData.Load();
        RedisClient.Instance.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
        PostgreSQLClient.Instance.Connect(DotEnv.Get<string>("DB_HOST"), DotEnv.Get<int>("DB_PORT"), DotEnv.Get<string>("DB_NAME"), DotEnv.Get<string>("DB_USER"), DotEnv.Get<string>("DB_PASSWORD"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MIN"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MAX"));


        var keys = TableData.Monsters.Keys.ToArray();

        int totalCount = spawnCount.Aggregate((acc, i) => acc + i);
        int[] spawnMonster = new int[totalCount];
        int currentCount = 0;
        for (var j = 0; j < spawnCount.Length; j++)
        {
            for (int i = 0; i < spawnCount[j]; i++)
            {
                spawnMonster[currentCount++] = keys[j];
            }
        }



        monsterController.AddMonster(1, spawnMonster);
        monsterController.AddMonster(2, spawnMonster);

    }
}
