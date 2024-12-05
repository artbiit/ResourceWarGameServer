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
    /// <summary>
    /// 데이터 디스패처 클래스
    /// 특정 키(TKey)를 기반으로 데이터를 비동기로 요청하여 제공하는 역할을 담당
    /// </summary>
    /// <typeparam name="TKey">데이터 요청에 사용되는 키 타입</typeparam>
    /// <typeparam name="TValue">요청 결과로 반환되는 값 타입</typeparam>
    public class DataDispatcher<TKey, TValue> : Singleton<DataDispatcher<TKey, TValue>> where TKey: unmanaged
    {
        // 데이터를 제공하는 프로바이더 함수
        private Func<TKey,UniTask<TValue>> dataProvider = null;

        /// <summary>
        /// 생성자
        /// 싱글톤 패턴 기반으로 초기화되고, 초기화 로그를 출력
        /// </summary>
        public DataDispatcher() :base()
        {
            Logger.Log($"{nameof(DataDispatcher<TKey, TValue>)} initialized");
        }

        /// <summary>
        /// 데이터 프로바이더를 설정
        /// 데이터를 제공하는 로직을 외부에서 주입받아 사용
        /// </summary>
        /// <param name="proivder">TKey를 받아 TValue를 반환하는 비동기 함수</param>
        public void SetProvider(Func<TKey,UniTask<TValue>> proivder)
        {
            dataProvider = proivder;
        }

        /// <summary>
        /// 데이터를 비동기로 요청
        /// 키를 기반으로 데이터를 요청하고, 프로바이더를 통해 결과를 반환
        /// 프로바이더가 성정되지 않은 경우 경고를 출력하고 기본값 반환
        /// </summary>
        /// <param name="key">요청에 사용될 키</param>
        /// <returns>요청 결과로 반환 되는 데이터</returns>
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
