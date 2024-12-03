using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{

    public enum MonsterPosition : int
    {
        Mellee = 1,
        Ranger = 2,
        Aircraft = 3,
    }

    public class MonsterTableData
    {
 
        public string Name { get; set; }
        public MonsterPosition Position { get; set; }
        public float Health { get; set; }
        public float Attack { get; set; } 
        public float Speed { get; set; }
        public float DetectRanged { get; set; }
        public float AttackRanged { get; set; }
        public int NeedWood { get; set; }
        public int NeedIron { get; set; }
        
    }
}
