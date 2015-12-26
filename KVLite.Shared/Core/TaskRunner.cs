// File name: TaskRunner.cs
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

using System;
using System.Threading.Tasks;

#if NET40
using System.Threading;
#endif

namespace PommaLabs.KVLite.Core
{
    static class TaskRunner
    {
        public static Task Run(Action action)
        {
#if NET40
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
#else
            return Task.Run(action);
#endif
        }

        public static Task<T> Run<T>(Func<T> func)
        {
#if NET40
            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
#else
            return Task.Run(func);
#endif
        }
    }
}
