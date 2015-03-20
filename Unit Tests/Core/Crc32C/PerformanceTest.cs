using System;
using System.Diagnostics;
using NUnit.Framework;
using PommaLabs.KVLite.Core.Crc32C;

namespace UnitTests.Core.Crc32C
{
    [TestFixture]
    public class PerformanceTest
    {
        [Test]
        public void Throughput()
        {
            var data = new byte[65536];
            var random = new Random();
            random.NextBytes(data);
            long total = 0;
            var stopwatch = new Stopwatch();
            uint crc = 0;
            stopwatch.Start();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(3))
            {
                var length = random.Next(data.Length + 1);
                var offset = random.Next(data.Length - length);
                crc = Crc32CAlgorithm.Append(crc, data, offset, length);
                total += length;
            }
            stopwatch.Stop();
            Console.WriteLine("Throughput: {0:0.0} GB/s", total / stopwatch.Elapsed.TotalSeconds / 1024 / 1024 / 1024);
        }
    }
}
