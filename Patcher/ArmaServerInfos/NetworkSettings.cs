
namespace ArmaServerInfo
{
    public class NetworkSettings
    {
        public string Host { get; set; }
        public int LocalPort { get; set; }
        public int RemotePort { get; set; }
        public int ReceiveTimeout { get; set; }

        public static NetworkSettings GetDefault()
		{
			return GetDefault("127.0.0.1");
		}
        public static NetworkSettings GetDefault(string host)
		{
			return GetDefault("127.0.0.1", 2302);
		}
        public static NetworkSettings GetDefault(string host, int port)
        {
            return new NetworkSettings
            {
                Host = host,
                LocalPort = 56800,
                RemotePort = port,
                ReceiveTimeout = 100
            };
        }
    }
}
