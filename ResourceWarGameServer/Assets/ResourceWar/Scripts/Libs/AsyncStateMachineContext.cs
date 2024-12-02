using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{


    public interface IAsyncState<T>
    {
        UniTask Enter(T context);
        UniTask Execute(T context);
        UniTask Exit(T context);
    }

    public class AsyncStateMachine<T>
    {
        private IAsyncState<T> currentState;
        private Dictionary<IAsyncState<T>, List<Transition>> transitions = new Dictionary<IAsyncState<T>, List<Transition>>();
        private List<Transition> globalTransitions = new List<Transition>();

        public async UniTask ChangeState(IAsyncState<T> newState, T context)
        {
            if (currentState != null)
            {
                await currentState.Exit(context);
            }

            currentState = newState;

            if (currentState != null)
            {
                await currentState.Enter(context);
            }
        }

        public async UniTask Update(T context)
        {
            foreach (var transition in globalTransitions)
            {
                if (transition.Condition())
                {
                    await ChangeState(transition.TargetState, context);
                    return;
                }
            }

            if (currentState != null && transitions.ContainsKey(currentState))
            {
                foreach (var transition in transitions[currentState])
                {
                    if (transition.Condition())
                    {
                        await ChangeState(transition.TargetState, context);
                        return;
                    }
                }
            }

            if (currentState != null)
            {
                await currentState.Execute(context);
            }
        }

        public void AddTransition(IAsyncState<T> fromState, IAsyncState<T> toState, Func<bool> condition)
        {
            if (!transitions.ContainsKey(fromState))
            {
                transitions[fromState] = new List<Transition>();
            }
            transitions[fromState].Add(new Transition(toState, condition));
        }

        public void AddGlobalTransition(IAsyncState<T> toState, Func<bool> condition)
        {
            globalTransitions.Add(new Transition(toState, condition));
        }

        private class Transition
        {
            public IAsyncState<T> TargetState { get; }
            public Func<bool> Condition { get; }

            public Transition(IAsyncState<T> targetState, Func<bool> condition)
            {
                TargetState = targetState;
                Condition = condition;
            }
        }
    }
}