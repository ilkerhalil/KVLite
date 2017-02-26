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
#elif (NET45 || NETSTD11)
                return Task.FromResult(0);
#elif NET46
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
#if (NET40 || NET45 || NETSTD11)
            var tcs = new TaskCompletionSource<TResult>();
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
#if (NET40 || NET45 || NETSTD11)
            var tcs = new TaskCompletionSource<TResult>();
            tcs.TrySetException(exception);
            return tcs.Task;
#elif NET46
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
#elif (NET45 || NET46 || NETSTD11)
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
    }
}
