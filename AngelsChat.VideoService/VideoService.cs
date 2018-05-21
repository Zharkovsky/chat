using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngelsChat.Shared.Data;
using AngelsChat.Shared.Operations;

namespace AngelsChat.VideoService
{
    public class VideoService : IVideoService
    {
        public void SendVideo(RoomDto room, List<byte[]> video)
        {
        }

        public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        {
        }

        public void SendVoice(RoomDto room, List<byte[]> voice)
        {
        }

        public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        {
        }
    }
}
