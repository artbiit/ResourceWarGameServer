using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ResourceWar.Server
{
    public class MonsterBehaviour : MonoBehaviour
    {
        #region Variables
        #region Stats
        public float MaxHealth;

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
        #endregion

        public int monsterId { get; private set; }
        public int teamId { get; private set; }

        private AsyncStateMachine<MonsterBehaviour> stateMachine = new();

        #endregion

        public void Init(int monsterId)
        {
            if (TableData.Monsters.TryGetValue(monsterId, out var monsterData) == false)
            {
                throw new System.InvalidOperationException($"Could not found monster in table : {monsterId}");
            }

            this.MaxHealth = monsterData.Health;
            this.CurrentHealth = monsterData.Health;
            this.Attack = monsterData.Attack;
            this.Speed = monsterData.Speed;
            this.AttackRanged = monsterData.AttackRanged;
            this.DetectRanged = monsterData.DetectRanged;
            

        }
    }
}
