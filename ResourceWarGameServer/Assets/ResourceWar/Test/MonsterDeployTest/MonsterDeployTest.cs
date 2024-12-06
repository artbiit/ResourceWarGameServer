using ResourceWar.Server;
using ResourceWar.Server.Lib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterDeployTest : MonoBehaviour
{
    public MonsterController monsterController;
    public int[] spawnCountTeam1 = new int[4];
    public int[] spawnCountTeam2 = new int[4];
    private void Awake()
    {
        DotEnv.Config();
        TableData.Load();
        RedisClient.Instance.Connect(DotEnv.Get<string>("REDIS_HOST"), DotEnv.Get<int>("REDIS_PORT"), DotEnv.Get<string>("REDIS_PASSWORD"));
        PostgreSQLClient.Instance.Connect(DotEnv.Get<string>("DB_HOST"), DotEnv.Get<int>("DB_PORT"), DotEnv.Get<string>("DB_NAME"), DotEnv.Get<string>("DB_USER"), DotEnv.Get<string>("DB_PASSWORD"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MIN"), DotEnv.Get<int>("DB_CONNECTION_LIMIT_MAX"));


    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return))
        {
            var keys = TableData.Monsters.Keys.ToArray();

            int totalCount = spawnCountTeam1.Aggregate((acc, i) => acc + i);
            int[] spawnMonster = new int[totalCount];
            int currentCount = 0;
            for (var j = 0; j < spawnCountTeam1.Length; j++)
            {
                for (int i = 0; i < spawnCountTeam1[j]; i++)
                {
                    spawnMonster[currentCount++] = keys[j];
                }
            }
            monsterController.AddMonster(1, spawnMonster);
            totalCount = spawnCountTeam2.Aggregate((acc, i) => acc + i);
            spawnMonster = new int[totalCount];
            currentCount = 0;
            for (var j = 0; j < spawnCountTeam2.Length; j++)
            {
                for (int i = 0; i < spawnCountTeam2[j]; i++)
                {
                    spawnMonster[currentCount++] = keys[j];
                }
            }
            monsterController.AddMonster(2, spawnMonster);
        }
    }
}
