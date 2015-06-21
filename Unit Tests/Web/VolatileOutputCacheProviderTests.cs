using NUnit.Framework;
using PommaLabs.KVLite.Web;

namespace UnitTests.Web
{
    [TestFixture]
    internal sealed class VolatileOutputCacheProviderTests
    {
        /// <summary>
        ///   Verifies issue #1.
        /// </summary>
        [Test]
        public void Get_ShouldReturnNullIfItemIsMissing()
        {
            var provider = new VolatileOutputCacheProvider();
            Assert.IsNull(provider.Get("X"));
        }
    }
}