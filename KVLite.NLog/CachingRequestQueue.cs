// File name: CachingRequestQueue.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

using global::NLog.Common;
using System.Collections.Generic;
using System.Threading;

namespace PommaLabs.KVLite.NLog
{
    /// <summary>
    ///   Asynchronous request queue.
    /// </summary>
    internal sealed class CachingRequestQueue
    {
        #region Constants

        /// <summary>
        ///   The partition prefix used by NLog caching target wrapper items.
        /// </summary>
        public const string ResponseCachePartition = "KVL.NLog.CTW_";

        #endregion Constants

        private readonly ICache _cache;
        private int _eventCount;
        private readonly Queue<AsyncLogEventInfo> _logEventInfoQueue = new Queue<AsyncLogEventInfo>();

        /// <summary>
        ///   Initializes a new instance of the <see cref="CachingRequestQueue"/> class.
        /// </summary>
        /// <param name="requestLimit">The request limit.</param>
        /// <param name="overflowAction">The overflow action.</param>
        public CachingRequestQueue(int requestLimit, CachingTargetWrapperOverflowAction overflowAction)
        {
            RequestLimit = requestLimit;
            OnOverflow = overflowAction;
        }

        /// <summary>
        ///   Gets or sets the request limit.
        /// </summary>
        public int RequestLimit { get; set; }

        /// <summary>
        ///   Gets or sets the action to be taken when there's no more room in the queue and another
        ///   request is enqueued.
        /// </summary>
        public CachingTargetWrapperOverflowAction OnOverflow { get; set; }

        /// <summary>
        ///   Gets the number of requests currently in the queue.
        /// </summary>
        public int RequestCount => _logEventInfoQueue.Count;

        /// <summary>
        ///   Enqueues another item. If the queue is overflown the appropriate action is taken as
        ///   specified by <see cref="OnOverflow"/>.
        /// </summary>
        /// <param name="logEventInfo">The log event info.</param>
        public void Enqueue(AsyncLogEventInfo logEventInfo)
        {
            var newEventCount = Interlocked.Increment(ref _eventCount);
            lock (this)
            {
                if (newEventCount >= RequestLimit)
                {
                    InternalLogger.Debug("Caching queue is full");
                    switch (OnOverflow)
                    {
                        case CachingTargetWrapperOverflowAction.Discard:
                            InternalLogger.Debug("Discarding incoming element");
                            Interlocked.Decrement(ref _eventCount);
                            break;

                        case CachingTargetWrapperOverflowAction.Grow:
                            InternalLogger.Debug("The overflow action is Grow, adding element anyway");
                            break;

                        case CachingTargetWrapperOverflowAction.Block:
                            while (_logEventInfoQueue.Count >= RequestLimit)
                            {
                                InternalLogger.Debug("Blocking because the overflow action is Block...");
                                Monitor.Wait(this);
                                InternalLogger.Trace("Entered critical section");
                            }

                            InternalLogger.Trace("Limit OK");
                            break;
                    }
                }

                _logEventInfoQueue.Enqueue(logEventInfo);
            }
        }

        /// <summary>
        ///   Dequeues a maximum of <c>count</c> items from the queue and adds returns the list
        ///   containing them.
        /// </summary>
        /// <param name="count">Maximum number of items to be dequeued.</param>
        /// <returns>The array of log events.</returns>
        public AsyncLogEventInfo[] DequeueBatch(int count)
        {
            var resultEvents = new List<AsyncLogEventInfo>();

            lock (this)
            {
                for (int i = 0; i < count; ++i)
                {
                    if (_logEventInfoQueue.Count <= 0)
                    {
                        break;
                    }

                    resultEvents.Add(_logEventInfoQueue.Dequeue());
                }

                if (OnOverflow == CachingTargetWrapperOverflowAction.Block)
                {
                    System.Threading.Monitor.PulseAll(this);
                }
            }

            return resultEvents.ToArray();
        }

        /// <summary>
        ///   Clears the queue.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                _logEventInfoQueue.Clear();
            }
        }
    }
}