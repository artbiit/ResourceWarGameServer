using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{
    public interface IDamageable 
    {
       public UnityEngine.Transform Transform { get; }
        public bool IsAlive { get; }
       public void TakeDamage(float damage, IDamageable hitUnit);

        public int GetID { get; }
    }
}
