using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace GServer
{
    public static class NetworkExtensions
    {
        public static int FreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) NetworkController.ShowException(new FormatException("Invalid endpoint format"));
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    NetworkController.ShowException(new FormatException("Invalid ip-adress"));
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    NetworkController.ShowException(new FormatException("Invalid ip-adress"));
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                NetworkController.ShowException(new FormatException("Invalid port"));
            }
            return new IPEndPoint(ip, port);
        }
    }
}
