using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceWar.Server;
using Cysharp.Threading.Tasks;

namespace ResourceWar.Server.Monster
{
    public class Idle : IAsyncState<MonsterStats>
    {
        public async UniTask Enter(MonsterStats stats)
        {
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterStats stats)
        {
            await UniTask.Yield();
        }

        public async UniTask Exit(MonsterStats stats)
        {
            await UniTask.Yield();
        }
    }
}
