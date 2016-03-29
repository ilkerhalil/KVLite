// File name: CachingTargetWrapper.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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

#region Original NLog copyright

// Copyright (c) 2004-2011 Jaroslaw Kowalski <jaak@jkowalski.net>
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice, this list of conditions
//   and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice, this list of
//   conditions and the following disclaimer in the documentation and/or other materials provided
//   with the distribution.
//
// * Neither the name of Jaroslaw Kowalski nor the names of its contributors may be used to endorse
//   or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion Original NLog copyright

using global::NLog;
using global::NLog.Common;
using global::NLog.Targets;
using global::NLog.Targets.Wrappers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace PommaLabs.KVLite.NLog
{
    /// <summary>
    ///   Provides asynchronous, buffered execution of target writes.
    /// </summary>
    /// <seealso href="https://github.com/nlog/nlog/wiki/AsyncWrapper-target">
    ///   Documentation on NLog Wiki
    /// </seealso>
    /// <remarks>
    ///   <p>
    ///     Asynchronous target wrapper allows the logger code to execute more quickly, by queueing
    ///     messages and processing them in a separate thread. You should wrap targets that spend a
    ///     non-trivial amount of time in their Write() method with asynchronous target to speed up logging.
    ///   </p>
    ///   <p>
    ///     Because asynchronous logging is quite a common scenario, NLog supports a shorthand
    ///     notation for wrapping all targets with AsyncWrapper. Just add async="true" to the
    ///     &lt;targets/&gt; element in the configuration file.
    ///   </p>
    ///   <code lang="XML">
    /// <![CDATA[
    /// <targets async="true">
    ///    ... your targets go here ...
    /// </targets>
    /// ]]>
    ///   </code>
    /// </remarks>
    /// <example>
    ///   <p>
    ///     To set up the target in the <a href="config.html">configuration file</a>, use the
    ///     following syntax:
    ///   </p>
    ///   <code lang="XML" source="examples/targets/Configuration File/AsyncWrapper/NLog.config"/>
    ///   <p>
    ///     The above examples assume just one target and a single rule. See below for a programmatic
    ///     configuration that's equivalent to the above config file:
    ///   </p>
    ///   <code lang="C#" source="examples/targets/Configuration API/AsyncWrapper/Wrapping File/Example.cs"/>
    /// </example>
    [Target("CachingWrapper", IsWrapper = true)]
    public sealed class CachingTargetWrapper : WrapperTargetBase
    {
        private readonly object lockObject = new object();
        private Timer lazyWriterTimer;
        private readonly Queue<AsyncContinuation> flushAllContinuations = new Queue<AsyncContinuation>();
        private readonly object continuationQueueLock = new object();

        /// <summary>
        ///   Initializes a new instance of the <see cref="CachingTargetWrapper"/> class.
        /// </summary>
        public CachingTargetWrapper()
            : this(null)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CachingTargetWrapper"/> class.
        /// </summary>
        /// <param name="wrappedTarget">The wrapped target.</param>
        public CachingTargetWrapper(Target wrappedTarget)
            : this(wrappedTarget, 10000, CachingTargetWrapperOverflowAction.Discard)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CachingTargetWrapper"/> class.
        /// </summary>
        /// <param name="wrappedTarget">The wrapped target.</param>
        /// <param name="queueLimit">Maximum number of requests in the queue.</param>
        /// <param name="overflowAction">The action to be taken when the queue overflows.</param>
        public CachingTargetWrapper(Target wrappedTarget, int queueLimit, CachingTargetWrapperOverflowAction overflowAction)
        {
            RequestQueue = new CachingRequestQueue(10000, CachingTargetWrapperOverflowAction.Discard, 180);
            BatchSize = 100;
            TimeToSleepBetweenBatches = 50;
            WrappedTarget = wrappedTarget;
            QueueLimit = queueLimit;
            OverflowAction = overflowAction;
        }

        /// <summary>
        ///   Gets or sets the number of log events that should be processed in a batch by the lazy
        ///   writer thread.
        /// </summary>
        /// <docgen category="Buffering Options" order="100"/>
        [DefaultValue(100)]
        public int BatchSize { get; set; }

        /// <summary>
        ///   Gets or sets the time in milliseconds to sleep between batches.
        /// </summary>
        /// <docgen category="Buffering Options" order="100"/>
        [DefaultValue(50)]
        public int TimeToSleepBetweenBatches { get; set; }

        /// <summary>
        ///   Gets or sets the action to be taken when the lazy writer thread request queue count
        ///   exceeds the set limit.
        /// </summary>
        /// <docgen category="Buffering Options" order="100"/>
        [DefaultValue("Discard")]
        public CachingTargetWrapperOverflowAction OverflowAction
        {
            get { return RequestQueue.OverflowAction; }
            set { RequestQueue.OverflowAction = value; }
        }

        /// <summary>
        ///   Gets or sets the limit on the number of requests in the lazy writer thread request queue.
        /// </summary>
        /// <docgen category="Buffering Options" order="100"/>
        [DefaultValue(10000)]
        public int QueueLimit
        {
            get { return RequestQueue.QueueLimit; }
            set { RequestQueue.QueueLimit = value; }
        }

        /// <summary>
        ///   Gets or sets the event lifetime, that is, how many seconds an event will be kept into the cache.
        /// </summary>
        public int EventLifetimeInSeconds
        {
            get { return RequestQueue.EventLifetimeInSeconds; }
            set { RequestQueue.EventLifetimeInSeconds = value; }
        }

        /// <summary>
        ///   Gets the queue of lazy writer thread requests.
        /// </summary>
        internal CachingRequestQueue RequestQueue { get; private set; }

        /// <summary>
        ///   Waits for the lazy writer thread to finish writing messages.
        /// </summary>
        /// <param name="asyncContinuation">The asynchronous continuation.</param>
        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            lock (continuationQueueLock)
            {
                flushAllContinuations.Enqueue(asyncContinuation);
            }
        }

        /// <summary>
        ///   Initializes the target by starting the lazy writer timer.
        /// </summary>
        protected override void InitializeTarget()
        {
            if (TimeToSleepBetweenBatches <= 0)
            {
                throw new NLogConfigurationException("The AysncTargetWrapper\'s TimeToSleepBetweenBatches property must be > 0");
            }

            base.InitializeTarget();
            RequestQueue.Clear();
            InternalLogger.Trace("AsyncWrapper '{0}': start timer", Name);
            lazyWriterTimer = new Timer(ProcessPendingEvents, null, Timeout.Infinite, Timeout.Infinite);
            StartLazyWriterTimer();
        }

        /// <summary>
        ///   Shuts down the lazy writer timer.
        /// </summary>
        protected override void CloseTarget()
        {
            StopLazyWriterThread();
            if (RequestQueue.EventCount > 0)
            {
                ProcessPendingEvents(null);
            }

            base.CloseTarget();
        }

        /// <summary>
        ///   Starts the lazy writer thread which periodically writes queued log messages.
        /// </summary>
        private void StartLazyWriterTimer()
        {
            lock (lockObject)
            {
                if (lazyWriterTimer != null)
                {
                    lazyWriterTimer.Change(TimeToSleepBetweenBatches, Timeout.Infinite);
                }
            }
        }

        /// <summary>
        ///   Stops the lazy writer thread.
        /// </summary>
        private void StopLazyWriterThread()
        {
            lock (lockObject)
            {
                if (lazyWriterTimer != null)
                {
                    lazyWriterTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    lazyWriterTimer = null;
                }
            }
        }

        /// <summary>
        ///   Adds the log event to asynchronous queue to be processed by the lazy writer thread.
        /// </summary>
        /// <param name="logEvent">The log event.</param>
        /// <remarks>
        ///   The <see cref="Target.PrecalculateVolatileLayouts"/> is called to ensure that the log
        ///   event can be processed in another thread.
        /// </remarks>
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            MergeEventProperties(logEvent.LogEvent);
            PrecalculateVolatileLayouts(logEvent.LogEvent);
            RequestQueue.Enqueue(logEvent);
        }

        private void ProcessPendingEvents(object state)
        {
            AsyncContinuation[] continuations;
            lock (continuationQueueLock)
            {
                continuations = flushAllContinuations.Count > 0
                    ? flushAllContinuations.ToArray()
                    : new AsyncContinuation[] { null };
                flushAllContinuations.Clear();
            }

            try
            {
                if (WrappedTarget == null)
                {
                    InternalLogger.Error("AsyncWrapper '{0}': WrappedTarget is NULL", Name);
                    return;
                }

                foreach (var continuation in continuations)
                {
                    var count = BatchSize;
                    if (continuation != null)
                    {
                        count = RequestQueue.EventCount;
                    }
                    InternalLogger.Trace("AsyncWrapper '{0}': Flushing {1} events.", Name, count);

                    if (RequestQueue.EventCount == 0)
                    {
                        if (continuation != null)
                        {
                            continuation(null);
                        }
                    }

                    var logEventInfos = RequestQueue.DequeueBatch(count);

                    if (continuation != null)
                    {
                        // write all events, then flush, then call the continuation
                        WrappedTarget.WriteAsyncLogEvents(logEventInfos);//, ex => this.WrappedTarget.Flush(continuation));
                    }
                    else
                    {
                        // just write all events
                        WrappedTarget.WriteAsyncLogEvents(logEventInfos);
                    }
                }
            }
            catch (Exception exception)
            {
                //InternalLogger.Error(exception, "AsyncWrapper '{0}': Error in lazy writer timer procedure.", Name);

                //if (exception.MustBeRethrown())
                //{
                //    throw;
                //}
            }
            finally
            {
                StartLazyWriterTimer();
            }
        }
    }
}