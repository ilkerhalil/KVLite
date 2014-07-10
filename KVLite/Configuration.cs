using System.Configuration;

namespace KVLite
{
    public sealed class Configuration : ConfigurationSection
    {
        internal static readonly Configuration Instance = ConfigurationManager.GetSection("KVLiteConfiguration") as Configuration;

        [ConfigurationProperty("CachePath", IsRequired = true)]
        public string CachePath
        {
            get { return (string)this["CachePath"]; }
        }
    }
}
