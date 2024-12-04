using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;

namespace ResourceWar.Server.Monster
{
    public class Attack : IAsyncState<MonsterBehaviour>
    {
        public bool attackable = true;
        public async UniTask Enter(MonsterBehaviour monster)
        {
            monster.NavMeshAgent.ResetPath();
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterBehaviour monster)
        {
            if (attackable)
            {
                _ = CoolDown();
                Debug.Log($"Hit! {monster.name} -> {monster.TargetUnit.Transform.name}");
                monster.TargetUnit.TakeDamage(monster.Attack, monster);
            }
          
            
            await UniTask.Yield();
        }

        private async UniTask CoolDown()
        {
            attackable = false;
            await UniTask.Delay(5000, delayTiming: PlayerLoopTiming.FixedUpdate);
            attackable = true;
        }

        public async UniTask Exit(MonsterBehaviour monster)
        {
            await UniTask.Yield();
        }
    }
}
