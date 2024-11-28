using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ResourceWar.Server.Lib { 
public class IntervalManager : MonoSingleton<IntervalManager> 
{
        private class IntervalTask
        {
            public Func<CancellationToken, UniTask> TaskFunc { get; }
            public float Interval { get; }
            public CancellationTokenSource CancellationTokenSource { get; }
            public bool IsRunning { get; set; }

            public IntervalTask(Func<CancellationToken, UniTask> taskFunc, float interval, CancellationTokenSource cts)
            {
                TaskFunc = taskFunc;
                Interval = interval;
                CancellationTokenSource = cts;
                IsRunning = false;
            }
        }

        private readonly Dictionary<string, List<IntervalTask>> _taskGroups = new Dictionary<string, List<IntervalTask>>();

        /// <summary>
        /// 태스크를 추가하고, 취소 가능한 CancellationToken을 반환합니다.
        /// </summary>
        /// <param name="key">태스크를 그룹화하기 위한 키</param>
        /// <param name="taskFunc">반복 실행할 작업</param>
        /// <param name="interval">인터벌 (초 단위)</param>
        /// <returns>취소 가능한 CancellationToken</returns>
        public CancellationToken AddTask(string key, Func<CancellationToken, UniTask> taskFunc, float interval)
        {
            var cts = new CancellationTokenSource();
            var newTask = new IntervalTask(taskFunc, interval, cts);

            if (!_taskGroups.ContainsKey(key))
            {
                _taskGroups[key] = new List<IntervalTask>();
            }

            _taskGroups[key].Add(newTask);
            RunTask(newTask).Forget(); // 독립적으로 실행

            return cts.Token;
        }

        /// <summary>
        /// 특정 태스크를 취소합니다.
        /// </summary>
        /// <param name="cancellationToken">취소할 작업의 CancellationToken</param>
        public void CancelTask(CancellationToken cancellationToken)
        {
            foreach (var group in _taskGroups.Values)
            {
                var task = group.Find(t => t.CancellationTokenSource.Token == cancellationToken);
                if (task != null)
                {
                    task.CancellationTokenSource.Cancel();
                    group.Remove(task);
                    break;
                }
            }
        }

        /// <summary>
        /// 특정 키에 해당하는 모든 태스크를 취소하고 제거합니다.
        /// </summary>
        /// <param name="key">취소할 태스크 그룹의 키</param>
        public void CancelAllTasksByKey(string key)
        {
            if (_taskGroups.TryGetValue(key, out var tasks))
            {
                foreach (var task in tasks)
                {
                    task.CancellationTokenSource.Cancel();
                }

                _taskGroups.Remove(key);
            }
        }

        /// <summary>
        /// 모든 태스크를 중단합니다.
        /// </summary>
        public void StopAllTasks()
        {
            foreach (var group in _taskGroups.Values)
            {
                foreach (var task in group)
                {
                    task.CancellationTokenSource.Cancel();
                }
            }

            _taskGroups.Clear();
        }

        private async UniTaskVoid RunTask(IntervalTask intervalTask)
        {
            intervalTask.IsRunning = true;
            var token = intervalTask.CancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    float nextExecutionTime = Time.realtimeSinceStartup + intervalTask.Interval;

                    // 작업 실행
                    await intervalTask.TaskFunc.Invoke(token);

                    // 다음 실행 시점까지 대기
                    float remainingTime = nextExecutionTime - Time.realtimeSinceStartup;
                    if (remainingTime > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(remainingTime), cancellationToken: token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 태스크가 취소될 때 예외 무시
            }
            finally
            {
                intervalTask.IsRunning = false;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.StopAllTasks();   
        }
    }
}