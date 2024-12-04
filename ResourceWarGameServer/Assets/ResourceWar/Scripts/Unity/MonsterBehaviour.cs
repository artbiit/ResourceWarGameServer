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
    public class MonsterBehaviour : MonoBehaviour, IDamageable
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

        public Transform Transform => this.transform;

        public NavMeshAgent NavMeshAgent;
        private AsyncStateMachine<MonsterBehaviour> stateMachine = new();
        public PhysicsScene PhysicsScene;
        /// <summary>
        /// 기본적으로 이동해야할 방향
        /// </summary>
        public Vector3 DefaultDirection;
        public IDamageable TargetUnit;
        #endregion

        private void Awake()
        {
            var die = new Die();
            var move = new Move();
            var chase = new Chase();
            var attack = new Attack();
            var idle = new Idle();
            stateMachine.AddGlobalTransition(die, () => this.IsAlive == false);
            stateMachine.AddTransition(idle, move, () => true);
            stateMachine.AddTransition(move, chase, () => TargetUnit?.IsAlive == true);
            stateMachine.AddTransition(move, attack, () => IsTargetInAttackRange());
            stateMachine.AddTransition(chase, attack, () => IsTargetInAttackRange());
            stateMachine.AddTransition(attack, chase, () => TargetUnit?.IsAlive == true && GetDistanceForTarget() > this.AttackRanged);
            stateMachine.AddTransition(attack, move, () => TargetUnit == null);
            stateMachine.AddTransition(chase, move, () => TargetUnit?.IsAlive == false);
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

        public void TakeDamage(float damage, IDamageable hitUnit)
        {
            this.currentHelath -= damage;
            if (this.TargetUnit == null)
            {
                this.TargetUnit = hitUnit;
            }
        }

        public bool IsTargetInAttackRange()
        {
            if (TargetUnit == null)
            {
                return false;
            }

            // 내 위치와 타겟 위치
            Vector3 myPosition = transform.position;
            Vector3 targetPosition = TargetUnit.Transform.position;

            // 타겟의 Collider 크기를 고려
            Collider targetCollider = TargetUnit.Transform.GetComponent<Collider>();
            float targetRadius = 0f;

            if (targetCollider != null)
            {
                // 타겟의 반지름 계산 (간단히 최대 크기를 사용)
                targetRadius = Mathf.Max(targetCollider.bounds.extents.x, targetCollider.bounds.extents.z);
            }

            // 거리 계산
            float distance = Vector3.Distance(myPosition, targetPosition);

            // 공격 범위 + 타겟의 크기 확인
            return distance <= AttackRanged + targetRadius;
        }

        public async UniTask Execute()
        {
            await stateMachine.Update(this);
        }

        public float GetDistanceForTarget()
        {
            float distance = float.MaxValue;
            if (TargetUnit != null)
            {
                distance = Vector3.Distance(transform.position, TargetUnit.Transform.position);
            }
            return distance;
        }

        public (bool needsToMove, Vector3 targetPosition) CalculateMovementToAttackRange()
        {
            if (TargetUnit == null)
            {
                return (false, Vector3.zero); // 타겟이 없으면 이동 필요 없음
            }

            // 내 위치와 타겟 위치
            Vector3 myPosition = transform.position;
            Vector3 targetPosition = TargetUnit.Transform.position;

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

            return (true, moveToPosition);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            DrawCircle(transform.position, this.DetectRanged);
            Gizmos.color = Color.red;
            DrawCircle(transform.position, this.AttackRanged);

            if (TargetUnit != null)
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
            float angleStep = 2 * Mathf.PI / segments; // 각도 간격
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }


    }
}
