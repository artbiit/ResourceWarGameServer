using Cysharp.Threading.Tasks;
using Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField]
        private Transform battleField;
        public Vector2 SpawnOffset = new Vector2(10f, 10f);
        /// <summary>
        /// Team - Position Count, 소환용 계수기
        /// </summary>
        private Dictionary<int, int[]> spawnedCounter = new();
        private int monsterAcc = 0;
        private void Awake()
        {
            EventDispatcher<MonsterController.Event, ReceivedPacket>.Instance.Subscribe(Event.AddNewTeam, Cheat_AddMonsters);
            monsterPool = new(monsterPool_OnCreate, monsterPool_OnGet, monsterPool_OnRelease, monsterPool_OnDestroy, true, 100, 300);
            for (int i = 0; i < TeamSpawnPoints.Length; ++i)
            {
                monsters.Add(i + 1, new List<MonsterBehaviour>());
            }
        }

        public async UniTask Cheat_AddMonsters(ReceivedPacket receivedPacket)
        {
            var payload = (C2SMonsterAddReq)receivedPacket.Payload;
            AddMonster(payload.TeamId, payload.Monsters.ToArray());
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

            List<UniTask> tasks = new List<UniTask>();
            while (!cts.IsCancellationRequested)
            {
                foreach (var monsterList in monsters.Values)
                {
                    foreach (var monster in monsterList)
                    {
                        tasks.Add(monster.Execute());
                    }
                }
                if (tasks.Count > 0)
                {
                    await UniTask.WhenAll(tasks);
               
                _ = SendSyncFieldUnitNoti();
                for (int i = 1; i <= monsters.Count; i++)
                {
                    for (int j = 0; j < monsters[i].Count; j++)
                    {
                        if (monsters[i][j].IsAlive == false)
                        {
                            monsterPool.Release(monsters[i][j]);
                            monsters[i].RemoveAt(j);
                            j--;
                        }
                    }
                }
                }
                await UniTask.Yield(cts.Token);
                tasks.Clear();
            }

            
        }

        private  async UniTask SendSyncFieldUnitNoti()
        {
            Packet packet = new Packet();
            packet.PacketType = PacketType.SYNC_FIELD_UNIT_NOTIFICATION;
            packet.Token = "";
            var payload = new S2CSyncFieldUnitNoti();
            foreach (var monsterPair in monsters)
            {
                var teamId = monsterPair.Key;
                foreach(var monster in monsterPair.Value)
                {
                    payload.Object.Add(new FieldUnit
                    {
                        Hp = (int)monster.CurrentHealth,
                        SpawnNumber = (int)monster.ID,
                        Position = monster.transform.position.FromVector(),
                        State = (int)monster.CurrentState,
                        TargetId = (monster.TargetUnit?.GetID ?? 0),
                        UnitType = (int)monster.Position,
                        TeamId = teamId,
                        UnitId = monster.monsterId
                    });
                }
                
            }
            packet.Payload = payload;
            await EventDispatcher<GameManager.GameManagerEvent, Packet>.Instance.NotifyAsync(GameManager.GameManagerEvent.SendPacketForAll, packet);
        }

        public void AddMonster(int teamId, int[] monsterIds)
        {
            
            var spawnPoint = TeamSpawnPoints[teamId-1];
            if (!spawnedCounter.ContainsKey(teamId))
            {
                spawnedCounter.Add(teamId, new int[System.Enum.GetNames(typeof(MonsterPosition)).Length]);
            }
            for (int i = 0; i < monsterIds.Length; ++i) {
                var monsterId = monsterIds[i];
                monsterPool.Get(out var monster);
                if (monster.Init(teamId, monsterId))
                {
#if UNITY_EDITOR
                    monster.name = $"[{teamId}]{TableData.Monsters[monsterId].Name}{monsters[teamId].Count}";
#endif
                    monster.ID = ++monsterAcc;
                    monsters[teamId].Add(monster);
                    Vector3 pos = spawnPoint.transform.position;
                    int monsterPosition = (int)monster.Position-1;
                    int count = spawnedCounter[teamId][monsterPosition]++;
                    pos -= spawnPoint.forward * SpawnOffset.y * (float)(monster.Position + (count / 9));
                    pos += spawnPoint.right * ((-(count & 1) | 1) * (((count + 1) >> 1) * SpawnOffset.x));
                    pos.y = transform.localScale.y;
                    //Debug.Log($"[{TableData.Monsters[monsterId].Name}]{i} - {spawnPoint.forward * SpawnOffset.y * (float)(monster.Position + (count / 3))} - {spawnPoint.right * SpawnOffset.x * (1f - ((i & 1) << 1)) * (i % 3)}\n{pos}");
                    monster.transform.position = pos;
                    //monster.NavMeshAgent.SetDestination(pos);
                    monster.gameObject.layer = LayerMask.NameToLayer($"Team{teamId}");
                    monster.DefaultDirection = spawnPoint.forward;

                }
                else
                {
                    monsterPool.Release(monster);
                }

            }

            for (int i = 0; i < spawnedCounter[teamId].Length; i++)
            {
                spawnedCounter[teamId][i] = 0;
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
