using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;

namespace ResourceWar.Server.Monster
{
    public class Chase : IAsyncState<MonsterBehaviour>
    {
        public async UniTask Enter(MonsterBehaviour monster)
        {
            Debug.Log($"{monster.name} Chase Enter -> {monster.TargetMonster.name}");
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterBehaviour monster)
        {
            var moveInfo = monster.CalculateMovementToAttackRange();
            if (moveInfo.needsToMove)
            {
                monster.NavMeshAgent.SetDestination(moveInfo.targetPosition);
            }
           
            await UniTask.Yield();
        }

        public async UniTask Exit(MonsterBehaviour monster)
        {
            await UniTask.Yield();
        }
    }
}
