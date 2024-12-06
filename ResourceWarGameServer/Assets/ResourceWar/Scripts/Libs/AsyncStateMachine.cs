using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceWar.Server
{


    public interface IAsyncState
    {
        UniTask Enter();
        UniTask Execute();
        UniTask Exit();
    }

    public class AsyncStateMachine
    {
        private IAsyncState currentState;
        private Dictionary<IAsyncState, List<Transition>> transitions = new Dictionary<IAsyncState, List<Transition>>();
        private List<Transition> globalTransitions = new List<Transition>();

        public async UniTask ChangeState(IAsyncState newState)
        {
            if (newState == currentState)
            {
                return;
            }

            if (currentState != null)
            {
                await currentState.Exit();
            }

            currentState = newState;

            if (currentState != null)
            {
                await currentState.Enter();
            }
        }

        public async UniTask Update()
        {
            foreach (var transition in globalTransitions)
            {
                if (transition.Condition())
                {
                    await ChangeState(transition.TargetState);
                    return;
                }
            }

            if (currentState != null && transitions.ContainsKey(currentState))
            {
                foreach (var transition in transitions[currentState])
                {
                    if (transition.Condition())
                    {
                        await ChangeState(transition.TargetState);
                        return;
                    }
                }
            }

            if (currentState != null)
            {
                await currentState.Execute();
            }
        }

        public void AddTransition(IAsyncState fromState, IAsyncState toState, Func<bool> condition)
        {
            if (!transitions.ContainsKey(fromState))
            {
                transitions[fromState] = new List<Transition>();
            }
            transitions[fromState].Add(new Transition(toState, condition));
        }

        public void AddGlobalTransition(IAsyncState toState, Func<bool> condition)
        {
            globalTransitions.Add(new Transition(toState, condition));
        }

        private class Transition
        {
            public IAsyncState TargetState { get; }
            public Func<bool> Condition { get; }

            public Transition(IAsyncState targetState, Func<bool> condition)
            {
                TargetState = targetState;
                Condition = condition;
            }
        }
    }
}