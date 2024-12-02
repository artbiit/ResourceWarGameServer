using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ResourceWar.Server
{
    public class MonsterController : MonoBehaviour
    {
        
        public enum Event
        {
            AddNewTeam
        }
        private Dictionary<int, List<MonsterBehaviour>> monsters = new();
        [SerializeField]
        private MonsterBehaviour monsterPrefab;

        CancellationTokenSource cts = new();

        private void Awake()
        {
            EventDispatcher<Event, int>.Instance.Subscribe(Event.AddNewTeam, AddNewTeam);
        }

        private async UniTask UpdateAsync()
        {
            if(cts == null)
            {
                cts = new CancellationTokenSource();
            }

            while (!cts.IsCancellationRequested)
            {
                foreach (var monster in monsters.Values)
                {
              
                }
                await UniTask.Yield(cts.Token);
            }

            
        }


        private  UniTask AddNewTeam(int teamId)
        {
            if(monsters.ContainsKey(teamId) == false)
            {
                monsters.Add(teamId, new List<MonsterBehaviour>());
            }
            return UniTask.CompletedTask;
        }
        public void AddMonster(int teamId, MonsterBehaviour monster)
        {
            monsters[teamId].Add(monster);
        }

        public void InstantiateMonster(int teamId)
        {
            var monster = GameObject.Instantiate(monsterPrefab);
            monsters[teamId].Add(monster);
        }

        private void OnDestroy()
        {
            if (cts != null) { 
            cts.Cancel();
            cts.Dispose();
            }
        }
    }
}
