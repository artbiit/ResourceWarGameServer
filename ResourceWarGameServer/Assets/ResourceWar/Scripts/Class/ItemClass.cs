using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public class ItemClass
    {
        private int id;
        private short type;
        private int max_stack_amount;
        public ItemClass(int id, short type, int max_stack_amount)
        {
            this.id = id;
            this.type = type;
            this.max_stack_amount= max_stack_amount;
        }
    }
}
