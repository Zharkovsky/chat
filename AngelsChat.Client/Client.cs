using System.ServiceModel;
using System;
using System.Collections.Generic;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using AngelsChat.Shared.Data;
using AngelsChat.Shared.Operations;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace AngelsChat.Client
{
    public delegate void RoomEvent(RoomDto room);
    public delegate void MessageEvent(RoomDto room, MessageDto message);
    public delegate void UserEvent(RoomDto room, UserDto message);
    public delegate void ImageEvent(ImageDto image, UserDto user);
    public delegate void MediaEvent(RoomDto room, List<byte[]> media, UserDto user);
    public delegate void ErrorEvent(string error, bool reconnect, bool logout);
    public delegate void FileRequestEvent(FilePartDto file);
    public delegate byte[] FileEvent(long recieved, long recieveBufferSize);

    public class ClientService : IChatDuplexCallback
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        IServerContract server;
        InstanceContext instanceContext;

        public ClientService()
        {
            instanceContext = new InstanceContext(this);

            //Log config
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);
            string folderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat");
            string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat\\ClientLog.txt");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            if (!File.Exists(filePath))
                File.Create(filePath);
            fileTarget.FileName = filePath;
            fileTarget.Layout = @"${longdate} ${level:upperCase=true} ${message} ${callsite:includeSourcePath=true} ${stacktrace:topFrames=10} ${exception:format=ToString} ${event-properties:property1}";
            var rule2 = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule2);
            LogManager.Configuration = config;
        }
        public void Open(string endpointAddress)
        {
            try
            {
                Log.Debug("Установка подключения");
                DuplexChannelFactory<IServerContract> clientChannelFactory;
                NetTcpBinding binding = new NetTcpBinding()
                {
                    MaxReceivedMessageSize = 1048576,
                    MaxBufferPoolSize = 1048576,
                    MaxBufferSize = 1048576,
                    Security = new NetTcpSecurity { Mode = SecurityMode.None }
                };
                EndpointAddress endpoint = new EndpointAddress(endpointAddress);
                clientChannelFactory = new DuplexChannelFactory<IServerContract>(instanceContext, binding, endpoint);
                server = clientChannelFactory.CreateChannel();
            }
            catch (Exception e)
            {
                ShowError("Проблемы при подключении к серверу.", true, true);
                Log.Error(e);
            }
        }

        public void Logout()
        {
            if (server != null)
                server.Logout();
        }

        private UserDto _user;
        public UserDto User
        {
            get => _user;
            set { _user = value; }
        }

        public event RoomEvent RoomRemovedEvent;
        protected void OnRoomRemove(RoomDto room) => RoomRemovedEvent?.Invoke(room);
        public void RoomRemoved(RoomDto room)
        {
            Log.Trace("Удаление комнаты {0}", room.Name);
            OnRoomRemove(room);
        }

        public event RoomEvent RoomUpdatedEvent;
        protected void OnRoomUpdate(RoomDto room) => RoomUpdatedEvent?.Invoke(room);
        public void RoomUpdated(RoomDto room)
        {
            Log.Trace("Обновление комнаты {0}", room.Name);
            OnRoomUpdate(room);
        }

        public event UserEvent UserInvited;
        protected void OnUserInvite(RoomDto room, UserDto user) => UserInvited?.Invoke(room, user);
        public void AddUser(RoomDto room, UserDto user)
        {
            Log.Trace("Добавление в комнату пользователя {0}", user.Name);
            OnUserInvite(room, user);
        }

        public event UserEvent UserRemoved;
        protected void OnUserRemove(RoomDto room, UserDto user) => UserRemoved?.Invoke(room, user);
        public void RemoveUser(RoomDto room, UserDto user)
        {
            Log.Trace("Удаление из комнаты пользователя {0}", user.Name);
            OnUserRemove(room, user);
        }

        public event UserEvent UserEntered;
        protected void OnUserEnter(RoomDto room, UserDto user) => UserEntered?.Invoke(room, user);
        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            Log.Trace("Добавление онлайн пользователя {0}", user.Name);
            OnUserEnter(room, user);
        }

        public event UserEvent UserOut;
        protected void OnUserOut(RoomDto room, UserDto user) => UserOut?.Invoke(room, user);
        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            Log.Trace("Удаление из онлайн пользователя {0}", user.Name);
            OnUserOut(room, user);
        }

        public event MessageEvent MessageRecived;
        protected void OnMessageRecived(RoomDto room, MessageDto message) => MessageRecived?.Invoke(room, message);
        public void PrintMessage(RoomDto room, MessageDto message)
        {
            Log.Trace("Сообщение: {0}", message.MessageText);
            OnMessageRecived(room, message);
        }

        public event ImageEvent UserImage;
        protected void OnChangeUserImage(ImageDto image, UserDto user) => UserImage?.Invoke(image, user);
        public void ChangeUserImage(UserDto user, ImageDto image)
        {
            Log.Trace("Изменение фото {0}", user.Name);
            OnChangeUserImage(image, user);
        }

        public event MediaEvent UserVideo;
        protected void SendUserVideo(RoomDto room, List<byte[]> video, UserDto user) => UserVideo?.Invoke(room, video, user);
        public void ShowVideo(RoomDto room, List<byte[]> video, UserDto user) => SendUserVideo(room, video, user);

        public event ErrorEvent Error;
        protected void OnError(string error, bool reconnect, bool logout) => Error?.Invoke(error, reconnect, logout);
        /// <summary>
        /// Если logout == true, то пользователь, независимо от reconnect переходит на окно авторизации
        /// </summary>
        public void ShowError(string error, bool reconnect = false, bool logout = false)
        {
            Log.Trace("Ошибка: {0}", error);
            OnError(error, reconnect, logout);
        }

        public bool CheckConnection()
        {
            try
            {
                Log.Trace("Проверка подключения");
                return server.CheckConnection();
            }
            catch (EndpointNotFoundException e)
            {
                Log.Error(e);
                ShowError("Проверьте правильность IP и порта");
                return false;
            }
            catch (NullReferenceException e)
            {
                Log.Error(e);
                ShowError("Проверьте правильность IP и порта");
                return false;
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Время подключения истекло, попробуйте заново");
                return false;
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу");
                return false;
            }
        }
        public bool Authorization(string name, string password)
        {
            try
            {
                Log.Trace("Авторизация {0}", name);
                User = server.Login(new LoginDto(name, password));
                return User != null;
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Время ожидания ответа сервера, попробуйте заново");
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу");
            }
            return false;
        }
        public bool Registration(string name, string password)
        {
            try
            {
                Log.Trace("Регистрация");
                User = server.SignUp(new LoginDto(name, password));
                return User != null;
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.");
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Время ожидания ответа сервера, попробуйте заново");
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу");
            }
            return false;
        }

        public void SendMessage(MessageDto message)
        {
            message.Owner = User;
            try
            {
                Log.Trace("Отправка сообщения");
                server.SendMessage(message);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Ошибка отправки сообщения. Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }
        public void SetImage(ImageDto image)
        {
            try
            {
                Log.Trace("Отправка картинки");
                User = server.SetImage(image);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true, true);
            }
        }

        private List<byte[]> SendingVideo = new List<byte[]>();
        DateTime last_video_sending = DateTime.MinValue;
        public void SendVideo(RoomDto room, byte[] video)
        {
            if (last_video_sending == DateTime.MinValue) last_video_sending = DateTime.Now;

            SendingVideo.Add(video);
            if (DateTime.Now - last_video_sending > TimeSpan.FromMilliseconds(40))
            {
                try
                {
                    server.SendVideo(room, SendingVideo);
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Log.Error(e);
                    SendUserVideo(room, null, User);
                    ShowError("Ошибка отправки видео. Проблемы при подключению к серверу.", true);
                }
                catch (TimeoutException e)
                {
                    Log.Error(e);
                    ShowError("Ошибка отправки видео. Проблемы при подключению к серверу.", true);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    ShowError("Ошибка отправки видео. Проблемы при подключению к серверу.", true, true);
                }
                SendingVideo.Clear();
                last_video_sending = DateTime.Now;
            }
        }

        private List<byte[]> SendingAudio = new List<byte[]>();
        DateTime last_audio_sending = DateTime.MinValue;
        public void SendVoice(RoomDto room, byte[] voice)
        {
            if (last_audio_sending == DateTime.MinValue) last_audio_sending = DateTime.Now;

            SendingAudio.Add(voice);
            if (DateTime.Now - last_audio_sending > TimeSpan.FromMilliseconds(20))
            {
                try
                {
                    server.SendVoice(room, SendingAudio);
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Log.Error(e);
                    SendUserVideo(room, null, User);
                    ShowError("Ошибка отправки аудио. Проблемы при подключению к серверу.", true);
                }
                catch (TimeoutException e)
                {
                    Log.Error(e);
                    ShowError("Ошибка отправки аудио. Проблемы при подключению к серверу.", true);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    ShowError("Ошибка отправки аудио. Проблемы при подключению к серверу.", true, true);
                }
                SendingAudio.Clear();
                last_audio_sending = DateTime.Now;
            }
        }

        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            try
            {
                Log.Trace("Загрузка online пользователей");
                return server.GetOnlineUsers(room);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Не удалось загрузить информацию онлайн пользователей", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return null;
        }
        public ImageDto GetAvatar(string name)
        {
            try
            {
                Log.Trace("Загрузка аватарок");
                return server.GetAvatar(name);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Не удалось загрузить фото пользователей", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return null;
        }
        public List<UserDto> GetAllUsers()
        {
            try
            {
                Log.Trace("Загрузка пользователей", true);
                return server.GetAllUsers();
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Не удалось загрузить пользователей", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return null;
        }
        public List<UserDto> GetUsers(RoomDto room)
        {
            try
            {
                Log.Trace("Загрузка пользователей", true);
                return server.GetUsers(room);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Не удалось загрузить пользователей", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return null;
        }
        public bool IsOnline(RoomDto room, string name)
        {
            try
            {
                Log.Trace("Проверка пользователя онлайн");
                return server.IsOnline(room, name);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Не удалось определить является ли пользователь онлайн", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return false;
        }
        public List<MessageDto> LoadMessages(RoomDto room, DateTime? date, int number = 5)
        {
            try
            {
                Log.Trace("Загрузка сообщений");
                return server.LoadMessages(room, number, date);
            }
            catch (CommunicationObjectFaultedException e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключению к серверу.", true);
            }
            catch (TimeoutException e)
            {
                Log.Error(e);
                ShowError("Не удалось загрузить сообщения", true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return null;
        }


        public int GetStreamCapacity() => server.GetMaxFileLength();
        public void PrepareFile(long length) => server.PrepareFile(length);
        public void SendPartOfFile(FilePartDto file) => server.SendPartOfFile(file);

        public long GetFile(FileMessageDto message) => server.GetFile(message);
        public byte[] DownloadFile(long recieved) => server.DownloadFile(recieved);
        public void DeleteFile() => server.DeleteFile();




        public event MediaEvent UserSound;
        protected void PlayUserSound(RoomDto room, List<byte[]> arr, UserDto user) => UserSound?.Invoke(room, arr, user);
        public void PlaySound(RoomDto room, List<byte[]> arr, UserDto user) => PlayUserSound(room, arr, user);

        public List<RoomDto> GetRooms()
        {
            try
            {
                return server.GetRooms();
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
            return null;
        }

        public RoomDto CreateRoom(RoomDto room)
        {
            try
            {
                return server.CreateRoom(room);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
                return null;
            }
        }

        public void RemoveRoom(RoomDto room)
        {
            try
            {
                server.RemoveRoom(room);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }

        public void UpdateRoom(RoomDto room)
        {
            try
            {
                server.UpdateRoom(room);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }

        public void InviteUser(RoomDto room, UserDto user)
        {
            try
            {
                server.InviteUser(room, user);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }
    }
}
