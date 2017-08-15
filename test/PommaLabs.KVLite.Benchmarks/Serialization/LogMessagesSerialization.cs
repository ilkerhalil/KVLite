// File name: LogMessagesSerialization.cs
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

using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.UnitTests;
using System.IO;

namespace PommaLabs.KVLite.Benchmarks.Serialization
{
    [Config(typeof(Config))]
    public class LogMessagesSerialization
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default);
                Add(CsvExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub, PlainExporter.Default);
                Add(new MemoryDiagnoser());
                Add(EnvironmentAnalyser.Default);
            }
        }

        private readonly ISerializer JsonSerializer = new JsonSerializer(new Newtonsoft.Json.JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None });
        private readonly ISerializer BsonSerializer = new BsonSerializer(new Newtonsoft.Json.JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None });
        private readonly ISerializer BinarySerializer = new BinarySerializer();
        private readonly Newtonsoft.Json.JsonSerializerSettings JsonNetOptions = new Newtonsoft.Json.JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None };
        private readonly Jil.Options JilOptions = new Jil.Options(prettyPrint: false);

        [Params(10, 100, 1000)]
        public int Count { get; set; }

        [Benchmark]
        public string JsonSerializer_Default()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                JsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                serializedStream.Position = 0L;
                using (var streamReader = new StreamReader(serializedStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        [Benchmark]
        public string BsonSerializer_Default()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                BsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                serializedStream.Position = 0L;
                using (var streamReader = new StreamReader(serializedStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        [Benchmark]
        public string BinarySerializer_Default()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                BinarySerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                serializedStream.Position = 0L;
                using (var streamReader = new StreamReader(serializedStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        [Benchmark]
        public string FastJson_Default() => fastJSON.JSON.ToJSON(LogMessage.GenerateRandomLogMessages(Count));

        [Benchmark]
        public string JsonNet_Default() => Newtonsoft.Json.JsonConvert.SerializeObject(LogMessage.GenerateRandomLogMessages(Count), JsonNetOptions);

        [Benchmark]
        public string Jil_Default() => Jil.JSON.Serialize(LogMessage.GenerateRandomLogMessages(Count), JilOptions);
    }
}
