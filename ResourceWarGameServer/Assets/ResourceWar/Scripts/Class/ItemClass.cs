using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public enum ItemType
    {
        Material = 0, // 재료
        Equipment = 1, // 장비
        Junk = 2       // 쓰레기
    }

    public class ItemClass
    {
        public int ItemCode { get; set; }   // 아이템 코드
        public string Name { get; set; }   // 아이템 이름
        public ItemType Type { get; set; } // 아이템 타입 (Enum)
        public string Prefab { get; set; } // 프리팹 이름

        public override string ToString()
        {
            return $"ItemCode: {ItemCode}, Name: {Name}, Type: {Type}, Prefab: {Prefab}";
        }
    }

}
