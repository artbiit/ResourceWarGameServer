using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public static class TableData 
    {
        public static Dictionary<int, MonsterTableData> Monsters { get; private set; }
        public static Dictionary<int, ItemTableData> Items { get; private set; }
        public static  void Load()
        {
            Monsters = CSVReader.ReadCsv<MonsterTableData>("monsters");
            Items = CSVReader.ReadCsv<ItemTableData>("items");
            Logger.Log("TableData loaded.");
        }
    }
}