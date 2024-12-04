using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class DataDispatcher<TKey, TValue> : Singleton<DataDispatcher<TKey, TValue>> where TKey: unmanaged
    {
        private Func<TKey,UniTask<TValue>> dataProvider = null;

        public DataDispatcher() :base()
        {
            Logger.Log($"{nameof(DataDispatcher<TKey, TValue>)} initialized");
        }

        public void SetProvider(Func<TKey,UniTask<TValue>> proivder)
        {
            dataProvider = proivder;
        }


        public async UniTask<TValue> RequestDataAsync(TKey key)
        {
            if(dataProvider != null)
            {
                return await dataProvider.Invoke(key);
            }
            Logger.LogWarning($"{nameof(DataDispatcher<TKey, TValue>)} No provider found.");
            return default(TValue);
        }



    }
}
