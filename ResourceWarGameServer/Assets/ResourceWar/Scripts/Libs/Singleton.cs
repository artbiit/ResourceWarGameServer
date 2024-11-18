using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace ResourceWar.Server
{
  
    public class Singleton<T> where T : class, new()
    {
        private static readonly Lazy<T> instance = new Lazy<T>(() => new T(), true);

        public static T Instance => instance.Value;

        protected Singleton()
        {
            if (instance.IsValueCreated)
            {
                throw new InvalidOperationException("Singleton instance already created.");
            }
        }
    }

}
