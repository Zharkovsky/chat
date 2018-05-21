using AngelsChat.Shared.Operations;
using AngelsChat.VideoService;
using System;
using System.ServiceModel;

namespace AngelsChat.ConsoleVideoService
{
    class Program
    {
        static void Main(string[] args)
        {

            //Create ServiceHost
            ServiceHost host = new ServiceHost(typeof(IVideoService));

            NetTcpBinding myBinding = new NetTcpBinding()
            {
                MaxReceivedMessageSize = 1048576,
                MaxBufferPoolSize = 1048576,
                MaxBufferSize = 1048576,
                Security = new NetTcpSecurity { Mode = SecurityMode.None }
            };

            //Add a service endpoint
            host.AddServiceEndpoint(typeof(IVideoService), myBinding, "net.tcp://localhost:9080/AngelsChat/VideoService");
            

            //Start the Service
            host.Open();
            Console.WriteLine("AngelsChat.VideoService is host at " + DateTime.Now.ToString());
            Console.WriteLine("Host is running... Press  key to stop");
            Console.ReadLine();
        }
    }
}
