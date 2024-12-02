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

        private void Awake()
        {
            monsterPool = new(monsterPool_OnCreate, monsterPool_OnGet, monsterPool_OnRelease, monsterPool_OnDestroy, true, 100, 300);
            EventDispatcher<Event, int>.Instance.Subscribe(Event.AddNewTeam, AddNewTeam);
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
        }
    }
}
