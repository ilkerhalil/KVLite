// File name: TaskHelper.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.Thrower;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Helpers for handling .NET tasks.
    /// </summary>
    public static class TaskHelper
    {
        /// <summary>
        ///   Gets a task that has already completed successfully.
        /// </summary>
        public static Task CompletedTask
        {
            get
            {
#if NET40
                return TaskEx.FromResult(0);
#elif NET45
                return Task.FromResult(0);
#else
                return Task.CompletedTask;
#endif
            }
        }

        /// <summary>
        ///   Gets a task that has been canceled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the task.</param>
        /// <returns>A task that has been canceled.</returns>
        public static Task<TResult> CanceledTask<TResult>(CancellationToken cancellationToken = default(CancellationToken))
        {
#if (NET40 || NET45)
            var tcs = new TaskCompletionSource<TResult>(cancellationToken);
            tcs.TrySetCanceled();
            return tcs.Task;
#else
            return Task.FromCanceled<TResult>(cancellationToken);
#endif
        }

        /// <summary>
        ///   Gets a task that has been blocked by given exception.
        /// </summary>
        /// <param name="exception">The exception that blocked the task.</param>
        /// <returns>A task that has been canceled.</returns>
        public static Task<TResult> ExceptionTask<TResult>(Exception exception)
        {
#if (NET40 || NET45)
            var tcs = new TaskCompletionSource<TResult>();
            tcs.TrySetException(exception);
            return tcs.Task;
#else
            return Task.FromException<TResult>(exception);
#endif
        }

        /// <summary>
        ///   Gets a task that bears given result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>A task that bears given result.</returns>
        public static Task<TResult> ResultTask<TResult>(TResult result)
        {
#if NET40
            return TaskEx.FromResult(result);
#else
            return Task.FromResult(result);
#endif
        }

        /// <summary>
        ///   Creates a task that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        ///   The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <returns>A task that represents the time delay.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The millisecondsDelay argument is less than -1.
        /// </exception>
        public static Task DelayTask(int millisecondsDelay)
        {
#if NET40
            return TaskEx.Delay(millisecondsDelay);
#else
            return Task.Delay(millisecondsDelay);
#endif
        }

        #region RunSync

        private static readonly TaskFactory MyTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        ///   Runs a task synchronously.
        /// </summary>
        /// <param name="task">The function returning the task.</param>
        public static void RunSync(Func<Task> task) => MyTaskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();

        /// <summary>
        ///   Runs a task synchronously.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="task">The function returning the task.</param>
        /// <returns>The result.</returns>
        public static T RunSync<T>(Func<Task<T>> task) => MyTaskFactory.StartNew(task).Unwrap().GetAwaiter().GetResult();

        #endregion RunSync

        #region RunAsync

        /// <summary>
        ///   Runs an action asynchronously.
        /// </summary>
        /// <param name="action">The action which should be run asynchronously.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The task.</returns>
        public static Task RunAsync(Action action, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<object>(cancellationToken);
            }
#if NET40
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
#else
            return Task.Run(action);
#endif
        }

        /// <summary>
        ///   Runs a function asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="func">The function which should be run asynchronously.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The task yielding the result.</returns>
        public static Task<TResult> RunAsync<TResult>(Func<TResult> func, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<TResult>(cancellationToken);
            }
#if NET40
            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
#else
            return Task.Run(func);
#endif
        }

        #endregion RunAsync

        #region TryFireAndForget

        /// <summary>
        ///   The maximum number of concurrent fire and forget tasks. Default value is equal to <see cref="Environment.ProcessorCount"/>.
        /// </summary>
        public static int FireAndForgetLimit { get; set; } = Environment.ProcessorCount;

        private const TaskContinuationOptions FireAndForgetFlags = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted;

        private static readonly Action<Task> DefaultErrorContination = t =>
        {
            try { t.Wait(); }
            catch { }
        };

        private static int FireAndForgetCount;

        /// <summary>
        ///   Tries to fire given action on a dedicated task, but it ensures that the number of
        ///   concurrent tasks is never greater than <see cref="FireAndForgetLimit"/>; if the number
        ///   of concurrent tasks is already too high, then given action is executed synchronously.
        ///
        ///   Optional error handler is invoked when given action throws an exception; if no handler
        ///   is specified, then the exception is swallowed.
        /// </summary>
        /// <param name="action">The action which might be fired and forgot.</param>
        /// <param name="handler">The optional error handler.</param>
        /// <returns>
        ///   True if given action has actually been fired and forgot; otherwise, it returns false.
        /// </returns>
        public static bool TryFireAndForget(Action action, Action<Exception> handler = null)
        {
            Raise.ArgumentNullException.IfIsNull(action, nameof(action));

            if (FireAndForgetCount >= FireAndForgetLimit)
            {
                // Run sync, cannot start a new task.
                RunSync(action, handler);
                return false;
            }

            if (Interlocked.Increment(ref FireAndForgetCount) > FireAndForgetLimit)
            {
                // Run sync, cannot start a new task.
                RunSync(action, handler);
                Interlocked.Decrement(ref FireAndForgetCount);
                return false;
            }

            RunAsync(() =>
            {
                action?.Invoke();
                Interlocked.Decrement(ref FireAndForgetCount);
            }, handler);
            return true;
        }

        /// <summary>
        ///   Tries to fire given action on a dedicated task, but it ensures that the number of
        ///   concurrent tasks is never greater than <see cref="FireAndForgetLimit"/>; if the number
        ///   of concurrent tasks is already too high, then given action is executed synchronously.
        ///
        ///   Optional error handler is invoked when given action throws an exception; if no handler
        ///   is specified, then the exception is swallowed.
        /// </summary>
        /// <param name="asyncAction">The action which might be fired and forgot.</param>
        /// <param name="handler">The optional error handler.</param>
        /// <returns>
        ///   True if given action has actually been fired and forgot; otherwise, it returns false.
        /// </returns>
        public static async Task<bool> TryFireAndForgetAsync(Func<Task> asyncAction, Action<Exception> handler = null)
        {
            Raise.ArgumentNullException.IfIsNull(asyncAction, nameof(asyncAction));

            if (FireAndForgetCount >= FireAndForgetLimit)
            {
                // Run sync, cannot start a new task.
                await RunSync(asyncAction, handler);
                return false;
            }

            if (Interlocked.Increment(ref FireAndForgetCount) > FireAndForgetLimit)
            {
                // Run sync, cannot start a new task.
                await RunSync(asyncAction, handler);
                Interlocked.Decrement(ref FireAndForgetCount);
                return false;
            }

            RunAsync(() =>
            {
                asyncAction?.Invoke();
                Interlocked.Decrement(ref FireAndForgetCount);
            }, handler);
            return true;
        }

        private static void RunAsync(Action action, Action<Exception> handler)
        {
#if !NET40
            var task = Task.Run(action);
#else
            var task = Task.Factory.StartNew(action, System.Threading.CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
#endif
            if (handler == null)
            {
                task.ContinueWith(DefaultErrorContination, FireAndForgetFlags);
            }
            else
            {
                task.ContinueWith(t => handler(t.Exception.GetBaseException()), FireAndForgetFlags);
            }
        }

        private static void RunAsync(Func<Task> asyncAction, Action<Exception> handler)
        {
#if !NET40
            var task = Task.Run(asyncAction);
#else
            var task = Task.Factory.StartNew(asyncAction, System.Threading.CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
#endif
            if (handler == null)
            {
                task.ContinueWith(DefaultErrorContination, FireAndForgetFlags);
            }
            else
            {
                task.ContinueWith(t => handler(t.Exception.GetBaseException()), FireAndForgetFlags);
            }
        }

        private static void RunSync(Action action, Action<Exception> handler)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                handler?.Invoke(ex);
            }
        }

        private static async Task RunSync(Func<Task> asyncAction, Action<Exception> handler)
        {
            try
            {
                await asyncAction?.Invoke();
            }
            catch (Exception ex)
            {
                handler?.Invoke(ex);
            }
        }

        #endregion TryFireAndForget
    }
}
