using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = ResourceWar.Server.Lib.Logger;

namespace ResourceWar.Server
{
    public class EventDispatcher<TKey, TEvent> : Singleton<EventDispatcher<TKey, TEvent>> where TKey : unmanaged
    {
        // 구독자를 저장하는 ConcurrentDictionary
        private readonly ConcurrentDictionary<TKey, List<Func<TEvent, UniTask>>> subscribers = new();

        // 생성자: EventDispatcher 초기화 메시지를 로깅
        public EventDispatcher() : base()
        {
            Logger.Log($"EventDispatcher<{typeof(TKey).Name}, {typeof(TEvent).Name} initialized.");
        }

        /// <summary>
        /// 특정 메시지 타입에 콜백을 구독
        /// </summary>
        /// <param name="messageType">메시지 타입</param>
        /// <param name="callback">실행할 콜백 함수</param>
        public void Subscribe(TKey messageType, Func<TEvent, UniTask> callback)
        {
            subscribers.AddOrUpdate(messageType,
                _ => new List<Func<TEvent, UniTask>> { callback }, // 새로운 구독자 추가
                (_, existingSubscribers) => {
                    lock (existingSubscribers)
                    {
                        existingSubscribers.Add(callback); // 기존 구독자 목록에 추가
                    }
                    return existingSubscribers;
                });
        }

        /// <summary>
        /// 특정 메시지 타입에 대한 구독 취소
        /// </summary>
        /// <param name="messageType">메시지 타입</param>
        /// <param name="callback">취소할 콜백 함수</param>
        public void Unsubcribe(TKey messageType, Func<TEvent, UniTask> callback)
        {
            if (this.subscribers.TryGetValue(messageType, out var existingSubscribers))
            {
                lock (existingSubscribers)
                {
                    existingSubscribers.Remove(callback); // 구독 취소
                    if (existingSubscribers.Count == 0)
                    {
                        subscribers.TryRemove(messageType, out _); // 목록 비우기
                    }
                }
            }
        }

        /// <summary>
        /// 특정 메시지 타입에 등록된 모든 콜백 함수 실행
        /// </summary>
        /// <param name="messageType">메시지 타입</param>
        /// <param name="result">전달할 데이터</param>
        public async UniTask NotifyAsync(TKey messageType, TEvent result)
        {
            if (subscribers.TryGetValue(messageType, out var existingSubscribers))
            {
                List<Func<TEvent, UniTask>> subscribersSnapshot;

                lock (existingSubscribers)
                {
                    subscribersSnapshot = new List<Func<TEvent, UniTask>>(existingSubscribers);
                }

                // 각 구독자 콜백 실행
                foreach (var subscriber in subscribersSnapshot)
                {
                    await subscriber.Invoke(result);
                }
            }
        }
    }
}