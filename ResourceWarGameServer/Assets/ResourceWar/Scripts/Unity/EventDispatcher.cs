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
    public class EventDispatcher<TKey,TEvent> : Singleton<EventDispatcher<TKey,TEvent>> where TKey : unmanaged
    {
        private readonly ConcurrentDictionary<TKey, List<Func<TEvent, UniTask>>> subscribers = new();

        public EventDispatcher() :base() {
            Logger.Log($"EventDispatcher<{typeof(TKey).Name}, {typeof(TEvent).Name} initialized.");
        }

        public void Subscribe(TKey messageType, Func<TEvent, UniTask> callback)
        {
            subscribers.AddOrUpdate(messageType,
                _ => new List<Func<TEvent, UniTask>> { callback },
                (_,existingSubscribers) => {
                lock (existingSubscribers)
                {
                    existingSubscribers.Add(callback);
                }
                return existingSubscribers;
            });
        }

        public void Unsubcribe(TKey MessageType, Func<TEvent, UniTask> callback)
        {
            if (this.subscribers.TryGetValue(MessageType, out var existingSubscribers))
            {
                lock (existingSubscribers)
                {
                    existingSubscribers.Remove(callback);
                    if(existingSubscribers.Count == 0)
                    {
                        subscribers.TryRemove(MessageType, out _);  
                    }
                }
            }
        }


        public async UniTask NotifyAsync(TKey messageType, TEvent result)
        {
            if(subscribers.TryGetValue(messageType, out var existingSubscribers)){
                List<Func<TEvent, UniTask>> subscribersSnapshot;

                lock (existingSubscribers)
                {
                    subscribersSnapshot = new List<Func<TEvent, UniTask>>(existingSubscribers);
                }


                foreach (var subscriber in subscribersSnapshot)
                {
                    await subscriber.Invoke(result);
                }
            }
        }
    }
}
