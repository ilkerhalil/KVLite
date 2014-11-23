using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Properties;

namespace UnitTests
{
    internal sealed class PersistentCacheTests : TestBase
    {
        private const string BlankPath = "   ";

        protected override ICache DefaultInstance
        {
           get { return PersistentCache.DefaultInstance; }
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddStatic_NotSerializableValue()
        {
            DefaultInstance.AddStatic(StringItems[0], new NotSerializableClass());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddStatic_DataContractValue()
        {
            DefaultInstance.AddStatic(StringItems[0], new DataContractClass());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_InvalidValues()
        {
            for (var i = 0; i < Settings.Default.PCache_DefaultInsertionCountBeforeCleanup; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_ValidValues()
        {
            for (var i = 0; i < Settings.Default.PCache_DefaultInsertionCountBeforeCleanup; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.AddMinutes(10));
            }
            Assert.AreEqual(Settings.Default.PCache_DefaultInsertionCountBeforeCleanup, DefaultInstance.Count());
        }

        [Test]
        public void Clean_InvalidValues()
        {
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear();
            Assert.AreEqual(0, DefaultInstance.Count());
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear(CacheReadMode.ConsiderExpirationDate);
            Assert.AreEqual(0, DefaultInstance.Count());
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void Clean_ValidValues()
        {
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.AddMinutes(10));
            }
            DefaultInstance.Clear();
            Assert.AreEqual(StringItems.Count, DefaultInstance.Count());
            DefaultInstance.Clear(CacheReadMode.ConsiderExpirationDate);
            Assert.AreEqual(StringItems.Count, DefaultInstance.Count());
            DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void NewCache_BlankPath()
        {
            try {
                new PersistentCache(new PersistentCacheSettings {CacheFile = BlankPath});
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_EmptyPath()
        {
            try {
                new PersistentCache(new PersistentCacheSettings {CacheFile = String.Empty});
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_NullPath()
        {
            try {
                new PersistentCache(new PersistentCacheSettings {CacheFile = null});
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }
    }

    internal sealed class NotSerializableClass
    {
        public string Pino = "Gino";
    }

    [DataContract]
    internal sealed class DataContractClass
    {
        [DataMember]
        public string Pino = "Gino";
    }
}