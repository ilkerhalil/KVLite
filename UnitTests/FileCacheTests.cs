using System;
using KVLite;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class FileCacheTests
    {
        private const string BlankPath = "   ";

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

        #region Miscellanea

        [Test]
        public void Constants_AppSettingsKey()
        {
            Assert.IsNotEmpty(Constants.AppSettingsKey.Trim());
        }

        #endregion
    }
}
