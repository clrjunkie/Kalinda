using System;
using System.Configuration;

namespace Kalinda.Configuration
{
    public class HttpServerElement : ConfigurationElement
    {
        [ConfigurationProperty("Host")]
        public string Host
        {
            get
            {
                try
                {
                    return this["Host"].ToString();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("AbsolutePath")]
        public string AbsolutePath
        {
            get
            {
                try
                {
                    return this["AbsolutePath"].ToString();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("SslEnabled")]
        public bool SslEnabled
        {
            get
            {
                try
                {
                    return Boolean.Parse(this["SslEnabled"].ToString());
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        [ConfigurationProperty("Port")]
        public int? Port
        {
            get
            {
                try
                {
                    return Int32.Parse(this["Port"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("ShutdownTimeout")]
        public int? ShutdownTimeout
        {
            get
            {
                try
                {
                    return Int32.Parse(this["ShutdownTimeout"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("MaxServerTasks")]
        public int MaxServerTasks
        {
            get
            {
                try
                {
                    return Int32.Parse(this["MaxServerTasks"].ToString());
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        [ConfigurationProperty("MinServerTasks")]
        public int MinServerTasks
        {
            get
            {
                try
                {
                    return Int32.Parse(this["MinServerTasks"].ToString());
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        [ConfigurationProperty("DrainEntityBodyTimeout")]
        public int? DrainEntityBodyTimeout
        {
            get
            {
                try
                {
                    return Int32.Parse(this["DrainEntityBodyTimeout"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("EntityBodyArrivalTimeout")]
        public int? EntityBodyArrivalTimeout
        {
            get
            {
                try
                {
                    return Int32.Parse(this["EntityBodyArrivalTimeout"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("HeaderWait")]
        public int? HeaderWait
        {
            get
            {
                try
                {
                    return Int32.Parse(this["HeaderWait"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("IdleConnectionTimeout")]
        public int? IdleConnectionTimeout
        {
            get
            {
                try
                {
                    return Int32.Parse(this["IdleConnectionTimeout"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("RequestInQueueTimeout")]
        public int? RequestInQueueTimeout
        {
            get
            {
                try
                {
                    return Int32.Parse(this["RequestInQueueTimeout"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("HttpRequestQueueLength")]
        public long? HttpRequestQueueLength
        {
            get
            {
                try
                {
                    return Int64.Parse(this["HttpRequestQueueLength"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [ConfigurationProperty("MinSendBytesPerSecond")]
        public uint? MinSendBytesPerSecond
        {
            get
            {
                try
                {
                    return UInt32.Parse(this["MinSendBytesPerSecond"].ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}