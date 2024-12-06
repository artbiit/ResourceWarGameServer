using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;

namespace ResourceWar.Server.Monster
{
    public class Die : IAsyncState<MonsterBehaviour>
    {

        public async UniTask Enter(MonsterBehaviour monster)
        {
            monster.CurrentState = MonsterBehaviour.State.Die;
            monster.NavMeshAgent.isStopped = true;
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterBehaviour monster)
        {
            await UniTask.Yield();
        }

        public async UniTask Exit(MonsterBehaviour monster)
        {
            await UniTask.Yield();
        }
    }
}
