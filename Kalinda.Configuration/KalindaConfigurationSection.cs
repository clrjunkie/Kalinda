using System.Configuration;

namespace Kalinda.Configuration
{
    public class KalindaConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("httpServer")]
        public HttpServerElement HttpServerElement
        {
            get { return (HttpServerElement)this["httpServer"]; }
        }
    }
}