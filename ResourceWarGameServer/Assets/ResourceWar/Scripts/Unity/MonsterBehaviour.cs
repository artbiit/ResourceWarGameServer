using System.Collections;
using System.Collections.Generic;
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

        private AsyncStateMachine<MonsterBehaviour> stateMachine = new();

        #endregion

        public void Init()
        {

        }

    }
}
