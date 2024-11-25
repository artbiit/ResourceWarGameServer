using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public class FurnaceClass : WorkshopClass
    {
        public FurnaceClass(int id, int gameTeamId, int itemId, int itemAmount = 0) 
            : base(id, gameTeamId, itemId, itemAmount)
        {
        }

        public override void StartProcessing()
        {
            base.StartProcessing();
        }

        public override void UpdateProgress(float deltaTime)
        {
            base.UpdateProgress(deltaTime);
            // 필요하다면 Furnace의 특정 프로세스 로직이 여기 들어가야함
            Console.WriteLine("Furnace is processing...");
        }
    }
}
