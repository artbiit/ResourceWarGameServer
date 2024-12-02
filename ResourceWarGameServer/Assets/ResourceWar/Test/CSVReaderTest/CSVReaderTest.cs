using ResourceWar.Server;
using ResourceWar.Server.Lib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class CSVReaderTest : MonoBehaviour
{
    private void Awake()
    {
        var monsterTables = CSVReader.ReadCsv<MonsterTableData>("monsters");

        Debug.Log(monsterTables?.Count);

        foreach (var monsterTable in monsterTables)
        {
            Debug.Log($"{monsterTable.Key} - {JsonConvert.SerializeObject(monsterTable.Value)}");
        }


    }
    
}
