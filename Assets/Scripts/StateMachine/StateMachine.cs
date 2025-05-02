using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TJ.Utils;
using UnityEngine;

public abstract class StateMachine<T> : Singleton<T> where T : Component
{
    public Type CurrentState { get; private set; }
    protected State _currentState;
    protected Dictionary<Type, State> _states;
    protected Type previousState;
    // Add transitioning flag
    public bool IsTransitioning { get; private set; }
    private Type stateToChange = null;

    public override void Awake()
    {
        base.Awake();
        var childStates = GetComponentsInChildren<State>();
        _states = new Dictionary<Type, State>();
        for (var i = 0; i < childStates.Length; i++)
        {
            _states.Add(childStates[i].GetType(), childStates[i]);
        }
    }

    public bool CanProcessInput()
    {
        return !IsTransitioning && _currentState != null;
    }

    public void ChangeState(State newState)
    {
        if (IsTransitioning || newState == _currentState) return;

        float delay = 0;
        StopAllCoroutines();
        stateToChange = newState.GetType();
        previousState = CurrentState;
        IsTransitioning = true;

        if (_currentState != null)
        {
            delay = _currentState.Exit();
        }

        StartCoroutine(ChangeStateOnDelay(delay, newState));
    }

    public void ChangeState(Type newStateType)
    {
        if (IsTransitioning) return;
        if (!_states.TryGetValue(newStateType, out var newState)) return;
        if (newStateType == CurrentState) return;

        float delay = 0;
        StopAllCoroutines();
        stateToChange = newStateType;
        previousState = CurrentState;
        IsTransitioning = true;

        if (_currentState != null)
        {
            delay = _currentState.Exit();
        }

        StartCoroutine(ChangeStateOnDelay(delay, newState));
    }

    public Type GetStateToChange()
    {
        return stateToChange;
    }

    public Type GetPreviousState()  // Add this getter
    {
        return previousState;
    }

    public State GetCurrentStateComponent()
    {
        return GetComponentsInChildren<State>()
            .FirstOrDefault(s => s.GetType() == CurrentState);
    }

    private IEnumerator ChangeStateOnDelay(float delay, State newState)
    {
        yield return new WaitForSeconds(delay);
        _currentState = newState;
        CurrentState = newState.GetType();
        _currentState.Enter();
        IsTransitioning = false;
    }
}