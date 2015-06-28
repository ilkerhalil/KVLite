using NUnit.Framework;
using PommaLabs.KVLite.Web;

namespace UnitTests.Web
{
    [TestFixture]
    internal sealed class PersistentOutputCacheProviderTests
    {
        /// <summary>
        ///   Verifies issue #1.
        /// </summary>
        [Test]
        public void Get_ShouldReturnNullIfItemIsMissing()
        {
            var provider = new PersistentOutputCacheProvider();
            Assert.IsNull(provider.Get("X"));
        }
    }
}