using PommaLabs.KVLite;

namespace UnitTests
{
   internal sealed class VolatileCacheTests : TestBase
   {
      protected override ICache DefaultInstance
      {
         get { return VolatileCache.DefaultInstance; }
      }
   }
}