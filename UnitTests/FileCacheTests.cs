using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KVLite;
using KVLite.Properties;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class FileCacheTests
    {
        private const int SmallItemCount = 10;
        private const int MediumItemCount = 100;
        private const int LargeItemCount = 1000;
        private const int MinItem = 10000;
        private const string BlankPath = "   ";

        private static readonly List<string> StringItems = (from x in Enumerable.Range(MinItem, LargeItemCount)
            select x.ToString(CultureInfo.InvariantCulture)).ToList();

        private FileCache _fileCache;

        [SetUp]
        public void SetUp()
        {
            _fileCache = new FileCache();
            _fileCache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _fileCache.Clear();
            _fileCache = null;
        }

        #region Construction

        [Test]
        public void NewCache_NullPath()
        {
            try
            {
                new FileCache(null);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.AreEqual(ErrorMessages.NullOrEmptyCachePath, ex.Message);
            }
        }

        [Test]
        public void NewCache_EmptyPath()
        {
            try
            {
                new FileCache(String.Empty);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.AreEqual(ErrorMessages.NullOrEmptyCachePath, ex.Message);
            }
        }

        [Test]
        public void NewCache_BlankPath()
        {
            try
            {
                new FileCache(BlankPath);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.AreEqual(ErrorMessages.NullOrEmptyCachePath, ex.Message);
            }
        }

        #endregion

        #region Get

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(_fileCache.Get(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_FullCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNotNull(_fileCache.Add(StringItems[i], StringItems[i], DateTime.UtcNow));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNotNull(_fileCache.Get(StringItems[i]));
            }
        }

        #endregion

        #region Settings

        [Test]
        public void Constants_AppSettings_CachePathKey()
        {
            Assert.IsNotEmpty(Settings.Default.AppSettings_CachePathKey.Trim());
        }

        [Test]
        public void Constants_AppSettings_MaxCacheSizeKey()
        {
            Assert.IsNotEmpty(Settings.Default.AppSettings_MaxCacheSizeKey.Trim());
        }

        #endregion
    }
}
