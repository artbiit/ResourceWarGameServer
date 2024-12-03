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

        public Move(MonsterBehaviour monster)
        {
            enemyLayer = LayerMask.NameToLayer($"Team{(monster.TeamId == 1 ? 2 : 1)}");
            allyLayer = LayerMask.NameToLayer($"Team{monster.TeamId}");
            physicsScene = monster.PhysicsScene;
        }

        public async UniTask Enter(MonsterBehaviour monster)
        {
           monster.NavMeshAgent.ResetPath();
            await UniTask.Yield();
        }

        public async UniTask Execute(MonsterBehaviour monster)
        {
            var transform = monster.transform;

        /*    // 정면에 아군이 있는지 확인
            if (physicsScene.Raycast(transform.position, transform.forward, out var allyHit, avoidanceDistance, 1 << allyLayer))
            {
                var ally = allyHit.collider.GetComponent<MonsterBehaviour>();
                if (ally && ally.IsAlive)
                {
                    await UniTask.Yield(); // 아군이 이동할 때까지 대기
                    return;
                }
            }*/

            // 적 탐지
            if (physicsScene.SphereCast(transform.position, monster.DetectRanged, transform.forward, out var enemyHit, layerMask: 1 << enemyLayer))
            {
                var target = enemyHit.collider.GetComponent<MonsterBehaviour>();
                if (target && target.IsAlive)
                {
                    monster.TargetMonster = target;
                    var moveInfo = monster.CalculateMovementToAttackRange();
                    if (moveInfo.needsToMove)
                    {
                        monster.NavMeshAgent.SetDestination(moveInfo.targetPosition);
                    }
                    return;
                }
            }

            // 기본 이동
            Vector3 targetPosition = transform.position + monster.DefaultDirection.normalized * monster.DetectRanged;
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
