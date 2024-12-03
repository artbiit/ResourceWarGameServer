using Cysharp.Threading.Tasks;
using ResourceWar.Server.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceWar.Server
{
    public class TimerManager<TKey>
    {
        private readonly Dictionary<TKey, CancellationTokenSource> timers = new();

        /// <summary>
        /// 새로운 타이머를 시작하거나 기존 타이머를 재설정합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="durationInSeconds"></param>
        /// <param name="onComplete"></param>
        public void StartTimer(TKey key, int durationInSeconds, Action<TKey> onComplete)
        {
            // 기존 타이머 취소
            if (timers.TryGetValue(key, out var existingToken))
            {
                existingToken.Cancel();
            }

            // 새로운 타이머 설정
            var cts = new CancellationTokenSource();
            timers[key] = cts;

            RunTimer(key, durationInSeconds, onComplete, cts.Token).Forget();
        }

        /// <summary>
        /// 특정 키의 타이머를 취소합니다.
        /// </summary>
        /// <param name="key"></param>
        public void CancelTimer(TKey key)
        {
            if (timers.TryGetValue(key, out var cts))
            {
                cts.Cancel();
                timers.Remove(key);
                Logger.Log($"Tumer for key [{key}] has been canceled.");
            }
        }

        /// <summary>
        /// 모든 타이머를 취소합니다.
        /// </summary>
        public void CancelAllTimers()
        {
            foreach (var cts in timers.Values)
            {
                cts.Cancel();
            }
            timers.Clear();
            Logger.Log("All timers have been canceled.");
        }

        /// <summary>
        /// 타이머가 지속 시간을 초과한 후 콜백을 실행합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="durationInSeconds"></param>
        /// <param name="onComplete"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask RunTimer(TKey key, int durationInSeconds, Action<TKey> onComplete, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(durationInSeconds * 1000, cancellationToken: token);

                // 타이머 만료 시 콜백 실행
                onComplete?.Invoke(key);

                // 타이머 제거
                timers.Remove(key);
            }
            catch(OperationCanceledException)
            {
                Logger.Log($"Timer for key[{key}] was canceled.");
            }
        }

        /// <summary>
        /// 특정 키의 타이머가 활성 상태인지 확인합니다.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsTimerActive(TKey key)
        {
            return timers.ContainsKey(key);
        }
    }
}
