using AngelsChat.Shared.Data;
using AngelsChat.Shared.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace AngelsChat.Server.Services
{
    public class VideoServiceClient : IVideoServiceDuplexCallback
    {
        IVideoService server;
        InstanceContext instanceContext;

        public VideoServiceClient()
        {
            instanceContext = new InstanceContext(this);

        }

        public void Open()
        {
            try
            {
                DuplexChannelFactory<IVideoService> clientChannelFactory;
                NetTcpBinding binding = new NetTcpBinding()
                {
                    MaxReceivedMessageSize = 1048576,
                    MaxBufferPoolSize = 1048576,
                    MaxBufferSize = 1048576,
                    Security = new NetTcpSecurity { Mode = SecurityMode.None }
                };
                EndpointAddress endpoint = new EndpointAddress("net.tcp://localhost:9080/AngelsChat/VideoService");
                clientChannelFactory = new DuplexChannelFactory<IVideoService>(instanceContext, binding, endpoint);
                server = clientChannelFactory.CreateChannel();
            }
            catch (Exception e)
            {
            }
        }

        public void PlaySound(RoomDto room, List<byte[]> voice, UserDto user)
        {
        }

        public void ShowVideo(RoomDto room, List<byte[]> video, UserDto user)
        {
            server.SendVideo(room, video);
        }
    }
}
