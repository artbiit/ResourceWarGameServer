using Cysharp.Threading.Tasks;
using ResourceWar.Server.Monster;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using Logger = ResourceWar.Server.Lib.Logger;
namespace ResourceWar.Server
{
    public class MonsterBehaviour : MonoBehaviour
    {
        #region Variables
        #region Stats
        public float MaxHealth;

        [SerializeField]
        private float currentHelath = 0;
        public bool IsAlive => currentHelath > 0;
        public float CurrentHealth
        {
            get => currentHelath;
            set
            {
                currentHelath = Mathf.Clamp(value, 0f, MaxHealth);
            }
        }

        public float Attack;
        public float Speed;
        public float DetectRanged;
        public float AttackRanged;
        public MonsterPosition Position;
        #endregion

        public int monsterId { get; private set; }
        public int TeamId { get; private set; }

        public NavMeshAgent NavMeshAgent;
        private AsyncStateMachine<MonsterBehaviour> stateMachine = new();
        public PhysicsScene PhysicsScene;
        /// <summary>
        /// 기본적으로 이동해야할 방향
        /// </summary>
        public Vector3 DefaultDirection;
        public MonsterBehaviour TargetMonster;
        #endregion

        private void Awake()
        {
            var die = new Die();
            var move = new Move(this);
            var chase = new Chase();
            var attack = new Attack();
            var idle = new Idle();
            stateMachine.AddGlobalTransition(die, () => this.IsAlive == false);
            stateMachine.AddTransition(idle, move, () => true);
            stateMachine.AddTransition(move, chase, () => GetDistanceForTarget() <= this.DetectRanged);
            stateMachine.AddTransition(move, attack, () => GetDistanceForTarget() <= this.AttackRanged);
            stateMachine.AddTransition(chase, attack, () => GetDistanceForTarget() <= this.AttackRanged);
            stateMachine.AddTransition(attack, chase, () => GetDistanceForTarget() > this.AttackRanged);
            stateMachine.AddTransition(chase, move, () => GetDistanceForTarget() > this.DetectRanged);
            _ = stateMachine.ChangeState(idle, this);


        }
        public bool Init(int teamId, int monsterId)
        {
            PhysicsScene = gameObject.scene.GetPhysicsScene();
            if (TableData.Monsters.TryGetValue(monsterId, out var monsterData) == false)
            {
               Logger.LogError($"Could not found monster in table : {monsterId}");
                return false;
            }
            this.TeamId = teamId;
            this.MaxHealth = monsterData.Health;
            this.CurrentHealth = monsterData.Health;
            this.Attack = monsterData.Attack;
            this.Speed = monsterData.Speed;
            this.AttackRanged = monsterData.AttackRanged;
            this.DetectRanged = monsterData.DetectRanged;
            this.Position = monsterData.Position;
            return true;
        }

        public async UniTask Execute()
        {
   
           await stateMachine.Update(this);
        }

        public float GetDistanceForTarget()
        {
            float distance =float.MaxValue;
            if (TargetMonster)
            {
                distance = Vector3.Distance(transform.position, TargetMonster.transform.position);

            }
            return distance;
        }

        public (bool needsToMove, Vector3 targetPosition) CalculateMovementToAttackRange()
        {
            if (TargetMonster == null)
            {
                return (false, Vector3.zero); // 타겟이 없으면 이동 필요 없음
            }

            // 내 위치와 타겟 위치
            Vector3 myPosition = transform.position;
            Vector3 targetPosition = TargetMonster.transform.position;

            // 두 위치 간 거리 계산
            float distance = Vector3.Distance(myPosition, targetPosition);

            if (distance <= AttackRanged)
            {
                // 공격 범위 내에 이미 타겟이 있으므로 이동할 필요 없음
                return (false, targetPosition);
            }

            // 공격 범위에 들어가도록 이동해야 함
            // 방향 벡터 계산
            Vector3 direction = (targetPosition - myPosition).normalized;

            // 공격 범위 가장자리에 해당하는 목표 좌표 계산
            Vector3 moveToPosition = targetPosition - direction * AttackRanged;

            // 네비메쉬에서 이동 가능 여부 확인
            if (NavMesh.SamplePosition(moveToPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                // 네비메쉬 상에서 이동 가능한 좌표 반환
                return (true, hit.position);
            }

            // 네비메쉬에서 유효하지 않으면 이동하지 않음
            return (false, Vector3.zero);
        }



        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            DrawCircle(transform.position, this.DetectRanged);
            Gizmos.color = Color.red;
            DrawCircle(transform.position, this.AttackRanged);

            if (TargetMonster)
            {
                
                var attackInfo = CalculateMovementToAttackRange();
                if (attackInfo.needsToMove)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(attackInfo.targetPosition, 0.2f); // 계산된 공격 위치 시각화

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, attackInfo.targetPosition); // 내 위치와 공격 위치를 연결
                }
            }
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            int segments = 50; // 원의 세그먼트 개수
            float angle = 0f;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

            for (int i = 1; i <= segments; i++)
            {
                angle += 2 * Mathf.PI / segments;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}
