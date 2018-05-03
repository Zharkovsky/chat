using AngelsChat.Server.Data.Entities;
using AngelsChat.Shared.Data;
using AngelsChat.Shared.Operations;
using System.Collections.Generic;

namespace AngelsChat.Server.ContractImplementations
{
    public interface IContract : IServerContract
    {
        User User { get; set; }

        void GetStarted();

        void ChangeUserImage(RoomDto room, UserDto user, ImageDto image);

        void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user);

        void AddUser(RoomDto room, UserDto user);

        void RemoveUser(RoomDto room, UserDto user);

        void AddOnlineUser(RoomDto room, UserDto user);

        void RemoveOnlineUser(RoomDto room, UserDto user);

        byte[] GetUserFile();

        void ClearUserFile();

        void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user);

    }
}
