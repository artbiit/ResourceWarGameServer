using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

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

        private CancellationTokenSource cts = null;

        private ObjectPool<MonsterBehaviour> monsterPool;
        [SerializeField]
        private Transform[] TeamSpawnPoints;

        private void Awake()
        {
            monsterPool = new(monsterPool_OnCreate, monsterPool_OnGet, monsterPool_OnRelease, monsterPool_OnDestroy, true, 100, 300);
            for (int i = 0; i < TeamSpawnPoints.Length; ++i)
            {
                monsters.Add(i + 1, new List<MonsterBehaviour>());
            }
        }

        

        private void OnEnable()
        {
            SwitchUpdate(true);
        }

        private void OnDisable()
        {
            SwitchUpdate(false);
        }

        private void SwitchUpdate(bool isOn)
        {
            if(isOn && cts == null)
            {
                cts = new CancellationTokenSource();
                UpdateAsync().Forget();
            }
            else if(!isOn && cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
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

        public void AddMonster(int teamId, int monsterId)
        {
            monsterPool.Get(out var monster);
            if (monster.Init(monsterId)) { 
            monsters[teamId].Add(monster);

                var spawnPoint = TeamSpawnPoints[teamId];
            }
            else
            {
                monsterPool.Release(monster);
            }
        }

        #region MonsterPool
        private MonsterBehaviour monsterPool_OnCreate()
        {
            var monster = GameObject.Instantiate(monsterPrefab);
            monster.gameObject.SetActive(false);
            return monster;
        }

        private void monsterPool_OnGet(MonsterBehaviour monster)
        {
            monster.gameObject.SetActive(true);
        }

        private void monsterPool_OnRelease(MonsterBehaviour monster)
        {
            monster.gameObject.SetActive(false);
        }

        private void monsterPool_OnDestroy(MonsterBehaviour monster)
        {

        }

        #endregion

        private void OnDestroy()
        {

            if (cts != null) { 
            cts.Cancel();
            cts.Dispose();
            }

            if (monsters != null)
            {
                foreach (var team in monsters.Values)
                {
                    foreach (var monster in team)
                    {
                        monsterPool.Release(monster);
                    }
                }
                monsters.Clear();
            }

            if (monsterPool != null)
            {
               
                monsterPool.Clear();
                monsterPool.Dispose();
                monsterPool = null;
            }
        }
    }
}
