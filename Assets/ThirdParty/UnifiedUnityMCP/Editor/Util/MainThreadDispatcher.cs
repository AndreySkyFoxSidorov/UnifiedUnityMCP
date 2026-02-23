using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEditor;

namespace Mcp.Editor.Util
{
    /// <summary>
    /// Executes actions on the main Unity thread.
    /// Safely bridges ASP.NET/HttpListener background threads with Unity API.
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

        public static void Initialize()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        public static void Shutdown()
        {
            EditorApplication.update -= Update;
        }

        private static void Update()
        {
            while (_executionQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "MainThreadDispatcher");
                }
            }
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread and returns a task that completes when the action finishes.
        /// </summary>
        public static Task InvokeAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            _executionQueue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Enqueues a func to be executed on the main thread and returns a task with the result.
        /// </summary>
        public static Task<T> InvokeAsync<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            _executionQueue.Enqueue(() =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            });
            return tcs.Task;
        }
    }
}
