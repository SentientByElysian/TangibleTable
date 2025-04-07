using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executeOnMainThread = new Queue<Action>();

    public static void ExecuteInUpdate(Action action)
    {
        lock (executeOnMainThread)
        {
            executeOnMainThread.Enqueue(action);
        }
    }

    public static Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        var tcs = new TaskCompletionSource<T>();

        ExecuteInUpdate(async () =>
        {
            try
            {
                var result = await func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public static Task InvokeAsync(Func<Task> func)
    {
        var tcs = new TaskCompletionSource<bool>();

        ExecuteInUpdate(async () =>
        {
            try
            {
                await func();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    void Update()
    {
        while (executeOnMainThread.Count > 0)
        {
            Action action;
            lock (executeOnMainThread)
            {
                action = executeOnMainThread.Dequeue();
            }
            action.Invoke();
        }
    }
}