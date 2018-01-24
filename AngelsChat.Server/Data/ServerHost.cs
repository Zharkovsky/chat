using AngelsChat.Server.ContractImplementations;
using ChatSettings = AngelsChat.Server.Settings.Settings;
using AngelsChat.Shared.Operations;
using NLog;
using System.ServiceModel;
using AngelsChat.Server.Core;

namespace AngelsChat.Server.Data
{
    public class ServerHost
    {
        ServerContract server;
        public ServerHost() { }
        public static ChatSettings Settings;
        public ServiceHost host;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public void Start(ChatSettings serverSettings)
        {
            Settings = serverSettings;
            NetTcpBinding myBinding = new NetTcpBinding()
            {
                MaxReceivedMessageSize = 1048576,
                MaxBufferPoolSize = 1048576,
                MaxBufferSize = 1048576,
                Security = new NetTcpSecurity { Mode = SecurityMode.None }
            };
            host = new ServiceHost(typeof(UserContract));
            host.AddServiceEndpoint(typeof(IServerContract), myBinding, $"net.tcp://{Settings.Connection.Ip}:{Settings.Connection.Port}/AngelsChat/");
            host.Open();
            server = new ServerContract();
        }

        public void Stop()
        {
            if (host.State != CommunicationState.Closed)
            host.Close();
        }
    }
}
