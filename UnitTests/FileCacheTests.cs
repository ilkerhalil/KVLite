using System;
using KVLite;
using KVLite.Properties;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class FileCacheTests
    {
        private const string BlankPath = "   ";

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
