using System.Collections;
using UnityEngine;
using ResourceWar.Server;
using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;

namespace ResourceWar.Server.Monster
{
    public class Move : IAsyncState<MonsterBehaviour>
    {
        private int enemyLayer = 0;
        private int allyLayer = 0;
        private float avoidanceDistance = 6f; // 우회 거리
        private float rayAngle = 45f; // 좌우 탐색 각도
        private PhysicsScene physicsScene;
        Collider[] castedEnemies = new Collider[3];
        public async UniTask Enter(MonsterBehaviour monster)
        {
            enemyLayer = 1 << LayerMask.NameToLayer($"Team{(monster.TeamId == 1 ? 2 : 1)}");
            allyLayer = 1 << LayerMask.NameToLayer($"Team{monster.TeamId}");
            physicsScene = monster.PhysicsScene;
            monster.NavMeshAgent.isStopped = false;
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterBehaviour monster)
        {
            var transform = monster.transform;

         /*   // 정면에 아군이 있는지 확인
            if (physicsScene.Raycast(transform.position, transform.forward, out var allyHit, avoidanceDistance,  allyLayer))
            {
                var ally = allyHit.collider.GetComponent<MonsterBehaviour>();
                if (ally && ally.IsAlive)
                {
                    await UniTask.Yield(); // 아군이 이동할 때까지 대기
                    return;
                }
            }*/

            int count = physicsScene.OverlapSphere(monster.transform.position, monster.DetectRanged, castedEnemies, enemyLayer, QueryTriggerInteraction.UseGlobal);

            for (int i = 0; i < count; i++)
            {
                var target = castedEnemies[i].GetComponent<IDamageable>();
                if (target != null && target.IsAlive)
                {
                    monster.TargetUnit = target;
                    var moveInfo = monster.CalculateMovementToAttackRange();
                    if (moveInfo.needsToMove)
                    {
                        monster.NavMeshAgent.SetDestination(moveInfo.targetPosition);
                    }
                    return;
                }

            }
     

            // 기본 이동
            Vector3 targetPosition = transform.position + monster.DefaultDirection.normalized * (monster.DetectRanged + monster.AttackRanged);
            if (monster.NavMeshAgent.pathPending || !monster.NavMeshAgent.SetDestination(targetPosition))
            {
                Debug.LogWarning("NavMesh 경로 탐색 실패: 기본 경로로 이동합니다.");
                monster.NavMeshAgent.Move(monster.DefaultDirection * monster.Speed * Time.deltaTime);
            }

            await UniTask.Yield();
        }

        public async UniTask Exit(MonsterBehaviour monster)
        {
            await UniTask.Yield();
        }

  
    }
}
