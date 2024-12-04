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
            monster.NavMeshAgent.isStopped = true;
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterBehaviour monster)
        {
            if(monster.Position == MonsterPosition.Mellee)
            {
                Debug.Log($"{monster.gameObject.name} -> {monster.TargetUnit}");
            }
            if (attackable && monster.TargetUnit != null)
            {
                _ = CoolDown();
                monster.TargetUnit.TakeDamage(monster.Attack, monster);
                if(monster.TargetUnit.IsAlive == false)
                {
                    monster.TargetUnit = null;
                }
            }
          
            
            await UniTask.Yield();
        }

        private async UniTask CoolDown()
        {
            attackable = false;
            await UniTask.Delay(1000, delayTiming: PlayerLoopTiming.FixedUpdate);
            attackable = true;
        }

        public async UniTask Exit(MonsterBehaviour monster)
        {
            await UniTask.Yield();
        }
    }
}
