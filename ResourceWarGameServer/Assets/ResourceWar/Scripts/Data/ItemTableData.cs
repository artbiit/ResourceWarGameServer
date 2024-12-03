using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public enum ItemTypes
    {
        SolidWood = 1,
        Ironstone,
        Wood,
        Iron,
        Garbage
    }
    public class ItemTableData
    {
        public string Name { get; set; }
        public ItemTypes ItemType { get; set; }
    }
}
