// File name: LogMessage.cs
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

using NLipsum.Core;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;

namespace PommaLabs.KVLite.Benchmarks.Models
{
    public sealed class LogMessage
    {
        private static readonly TRandom Random = new TRandom();
        private static readonly LogLevel[] LogLevels = { LogLevel.Debug, LogLevel.Error, LogLevel.Fatal, LogLevel.Info, LogLevel.Trace, LogLevel.Warn };
        private static readonly LipsumGenerator LipsumGenerator = new LipsumGenerator();

        public LogLevel Level { get; set; }

        public string ShortMessage { get; set; }

        public string LongMessage { get; set; }

        public static IEnumerable<LogMessage> GenerateRandomLogMessages(int count) => Enumerable.Range(0, count).Select(_ => new LogMessage
        {
            Level = Random.Choice(LogLevels),
            ShortMessage = LipsumGenerator.GenerateSentences(1)[0],
            LongMessage = LipsumGenerator.GenerateHtml(Random.Next(5, 10))
        });
    }

    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }
}