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
using Microsoft.AspNet.SignalR.Client;
using System.Diagnostics;

namespace AngelsChat.Client
{
    public delegate void UserProfileEvent(UserDto user, string name);
    public delegate void RoomEvent(RoomDto room);
    public delegate void MessageEvent(RoomDto room, MessageDto message);
    public delegate void UserEvent(RoomDto room, UserDto message);
    public delegate void ImageEvent(ImageDto image, UserDto user);
    public delegate void MediaEvent(RoomDto room, List<byte[]> media, UserDto user);
    public delegate void ErrorEvent(string error, bool reconnect, bool logout);
    public delegate void FileRequestEvent(FilePartDto file);
    public delegate byte[] FileEvent(long recieved, long recieveBufferSize);

    public class SignalRClientService : IClientService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public UserDto User { get; set; }

        public event UserProfileEvent UserProfileUpdateEvent;
        public event RoomEvent RoomRemovedEvent;
        public event RoomEvent RoomUpdatedEvent;
        public event UserEvent UserInvited;
        public event UserEvent UserRemoved;
        public event UserEvent UserEntered;
        public event UserEvent UserOut;
        public event MessageEvent MessageRecived;
        public event ImageEvent UserImage;
        public event MediaEvent UserVideo;
        public event MediaEvent UserSound;
        public event ErrorEvent Error;

        private IHubProxy _systemHub;

        public SignalRClientService()
        {
            ConfigLog();
        }

        private void ConfigLog()
        {
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

        protected void OnUserProfileUpdate(UserDto user, string name) => UserProfileUpdateEvent?.Invoke(user, name);
        protected void OnRoomRemove(RoomDto room) => RoomRemovedEvent?.Invoke(room);
        protected void OnRoomUpdate(RoomDto room) => RoomUpdatedEvent?.Invoke(room);
        protected void OnUserInvite(RoomDto room, UserDto user) => UserInvited?.Invoke(room, user);
        protected void OnUserRemove(RoomDto room, UserDto user) => UserRemoved?.Invoke(room, user);
        protected void OnUserEnter(RoomDto room, UserDto user) => UserEntered?.Invoke(room, user);
        protected void OnUserOut(RoomDto room, UserDto user) => UserOut?.Invoke(room, user);
        protected void OnMessageRecived(RoomDto room, MessageDto message) => MessageRecived?.Invoke(room, message);
        protected void OnChangeUserImage(ImageDto image, UserDto user) => UserImage?.Invoke(image, user);
        protected void SendUserVideo(RoomDto room, List<byte[]> video, UserDto user) => UserVideo?.Invoke(room, video, user);
        protected void PlayUserSound(RoomDto room, List<byte[]> arr, UserDto user) => UserSound?.Invoke(room, arr, user);
        protected void OnError(string error, bool reconnect, bool logout) => Error?.Invoke(error, reconnect, logout);

        public void Open(string url = "http://localhost:9080/")
        {
            url = "http://localhost:9080/";
            var _hubConnection = new HubConnection(url);
            _systemHub = _hubConnection.CreateHubProxy("UserContract");
            _hubConnection.Start().Wait();

            _systemHub.On<RoomDto, MessageDto>(nameof(PrintMessage), PrintMessage);
            _systemHub.On<RoomDto, UserDto>(nameof(AddUser), AddUser);
            _systemHub.On<RoomDto, UserDto>(nameof(RemoveUser), RemoveUser);
            _systemHub.On<RoomDto, UserDto>(nameof(AddOnlineUser), AddOnlineUser);
            _systemHub.On<RoomDto, UserDto>(nameof(RemoveOnlineUser), RemoveOnlineUser);
            _systemHub.On<UserDto, ImageDto>(nameof(ChangeUserImage), ChangeUserImage);
            _systemHub.On<RoomDto, List<byte[]>, UserDto> (nameof(ShowVideo), ShowVideo);
            _systemHub.On<RoomDto, List<byte[]>, UserDto> (nameof(PlaySound), PlaySound);
            _systemHub.On<RoomDto>(nameof(RoomRemoved), RoomRemoved);
            _systemHub.On<RoomDto>(nameof(RoomUpdated), RoomUpdated);
            _systemHub.On<UserDto, string>(nameof(UpdateProfileOnClientSide), UpdateProfileOnClientSide);

        }

        public void Logout()
        {
            if (_systemHub != null)
                _systemHub.Invoke("Logout");
        }

        public void UpdateProfileOnClientSide(UserDto user, string name)
        {
            OnUserProfileUpdate(user, name);
        }

        public void RoomRemoved(RoomDto room)
        {
            Log.Trace("Удаление комнаты {0}", room.Name);
            OnRoomRemove(room);
        }

        public void RoomUpdated(RoomDto room)
        {
            Log.Trace("Обновление комнаты {0}", room.Name);
            OnRoomUpdate(room);
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            Log.Trace("Добавление в комнату пользователя {0}", user.Name);
            OnUserInvite(room, user);
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            Log.Trace("Удаление из комнаты пользователя {0}", user.Name);
            OnUserRemove(room, user);
        }


        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            Log.Trace("Добавление онлайн пользователя {0}", user.Name);
            OnUserEnter(room, user);
        }


        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            Log.Trace("Удаление из онлайн пользователя {0}", user.Name);
            OnUserOut(room, user);
        }

        public void PrintMessage(RoomDto room, MessageDto message)
        {
            Log.Trace("Сообщение: {0}", message.MessageText);
            OnMessageRecived(room, message);
        }

        public void ChangeUserImage(UserDto user, ImageDto image)
        {
            Log.Trace("Изменение фото {0}", user.Name);
            OnChangeUserImage(image, user);
        }

        public void ShowVideo(RoomDto room, List<byte[]> video, UserDto user) => SendUserVideo(room, video, user);

        public void PlaySound(RoomDto room, List<byte[]> arr, UserDto user) => PlayUserSound(room, arr, user);

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
                return _systemHub.Invoke<bool>("CheckConnection").WaitResult();
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
                User = _systemHub.Invoke<UserDto>("Login", new LoginDto(name, password)).WaitResult();
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
                User = _systemHub.Invoke<UserDto>("SignUp", new LoginDto(name, password)).WaitResult();
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
        public bool UpdateProfile(string name, string password)
        {
            try
            {
                Log.Trace("Обновление профиля {0}", name);
                User = _systemHub.Invoke<UserDto>("UpdateProfile", new LoginDto(name, password)).WaitResult();
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
        public void SendMessage(MessageDto message)
        {
            message.Owner = User;
            try
            {
                Log.Trace("Отправка сообщения");
                _systemHub.Invoke("SendMessage", message);
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
                User = _systemHub.Invoke<UserDto>("SetImage", image).WaitResult();
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

        public void AddRoomForVideo(RoomDto room)
        {
            try
            {
                _systemHub.Invoke("AddRoomForVideo", room);
            }
            catch (Exception ex)
            {

            }
        }
        public void RemoveRoomFromVideo(RoomDto room)
        {
            try
            {
                _systemHub.Invoke("RemoveRoomFromVideo", room);
            }
            catch (Exception ex)
            {

            }
        }

        private List<byte[]> _sendingVideo = new List<byte[]>();
        private DateTime _lastVideoSending = DateTime.MinValue;
        public void SendVideo(RoomDto room, byte[] video)
        {
            if (_lastVideoSending == DateTime.MinValue) _lastVideoSending = DateTime.Now;

            _sendingVideo.Add(video);
            if (DateTime.Now - _lastVideoSending > TimeSpan.FromMilliseconds(40))
            {
                try
                {
                    _systemHub.Invoke("SendVideo", room, _sendingVideo);
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Log.Error(e);
                    _systemHub.Invoke("SendUserVideo", room, null, User);
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
                _sendingVideo.Clear();
                _lastVideoSending = DateTime.Now;
            }
        }

        private List<byte[]> _sendingAudio = new List<byte[]>();
        private DateTime _lastAudioSending = DateTime.MinValue;
        public void SendVoice(byte[] voice)
        {
            if (_lastAudioSending == DateTime.MinValue) _lastAudioSending = DateTime.Now;

            _sendingAudio.Add(voice);
            if (DateTime.Now - _lastAudioSending > TimeSpan.FromMilliseconds(20))
            {
                try
                {
                    _systemHub.Invoke("SendVoice", _sendingAudio);
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Log.Error(e);
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
                _sendingAudio.Clear();
                _lastAudioSending = DateTime.Now;
            }
        }

        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            try
            {
                Log.Trace("Загрузка online пользователей");
                return _systemHub.Invoke<List<UserDto>>("GetOnlineUsers", room).WaitResult();
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
                return _systemHub.Invoke<ImageDto>("GetAvatar", name).WaitResult();
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
                return _systemHub.Invoke<List<UserDto>>("GetAllUsers").WaitResult();
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
                return _systemHub.Invoke<List<UserDto>>("GetUsers", room).WaitResult();
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
                return _systemHub.Invoke<bool>("IsOnline", room, name).WaitResult();
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
                return _systemHub.Invoke<List<MessageDto>>("LoadMessages", room, number, date).WaitResult();
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


        public int GetStreamCapacity() => _systemHub.Invoke<int>("GetMaxFileLength").WaitResult();
        public void PrepareFile(long length) => _systemHub.Invoke("PrepareFile", length); 
        public void SendPartOfFile(FilePartDto file) => _systemHub.Invoke("SendPartOfFile", file); 

        public long GetFile(FileMessageDto message) => _systemHub.Invoke<long>("GetFile", message).WaitResult(); 
        public byte[] DownloadFile(long recieved) => _systemHub.Invoke<byte[]>("DownloadFile", recieved).WaitResult(); 
        public void DeleteFile() => _systemHub.Invoke("DeleteFile");

        public List<RoomDto> GetRooms()
        {
            try
            {
                var r = _systemHub.Invoke<List<RoomDto>>("GetRooms").WaitResult();
                return r;
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
                return _systemHub.Invoke<RoomDto>("CreateRoom", room).WaitResult();
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
                _systemHub.Invoke("RemoveRoom", room);
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
                _systemHub.Invoke("UpdateRoom", room);
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
                _systemHub.Invoke("InviteUser", room, user);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }
        public void KickUser(RoomDto room, UserDto user)
        {
            try
            {
                _systemHub.Invoke("KickUser", room, user);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }
    }

    public interface IClientService : IChatDuplexCallback
    {
        UserDto User { get; set; }

        event UserProfileEvent UserProfileUpdateEvent;
        event RoomEvent RoomRemovedEvent;
        event RoomEvent RoomUpdatedEvent;
        event UserEvent UserInvited;
        event UserEvent UserRemoved;
        event UserEvent UserEntered;
        event UserEvent UserOut;
        event MessageEvent MessageRecived;
        event ImageEvent UserImage;
        event MediaEvent UserVideo;
        event MediaEvent UserSound;
        event ErrorEvent Error;

        void Open(string endpointAdress);
        void Logout();

        void ShowError(string error, bool reconnect = false, bool logout = false);
        bool CheckConnection();
        bool Authorization(string name, string password);
        bool Registration(string name, string password);
        bool UpdateProfile(string name, string password);
        void SendMessage(MessageDto message);
        void SetImage(ImageDto image);
        void AddRoomForVideo(RoomDto room);
        void RemoveRoomFromVideo(RoomDto room);
        void SendVideo(RoomDto room, byte[] video);
        void SendVoice(byte[] voice);
        List<UserDto> GetOnlineUsers(RoomDto room);
        ImageDto GetAvatar(string name);
        List<UserDto> GetAllUsers();
        List<UserDto> GetUsers(RoomDto room);
        bool IsOnline(RoomDto room, string name);
        List<MessageDto> LoadMessages(RoomDto room, DateTime? date, int number = 5);
        int GetStreamCapacity();
        void PrepareFile(long length);
        void SendPartOfFile(FilePartDto file);
        byte[] DownloadFile(long recieved);
        void DeleteFile();
        List<RoomDto> GetRooms();
        void UpdateRoom(RoomDto room);
        void InviteUser(RoomDto room, UserDto user);
        void KickUser(RoomDto room, UserDto user);
    }

    public class ClientService : IClientService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public UserDto User { get; set; }

        public event UserProfileEvent UserProfileUpdateEvent;
        public event RoomEvent RoomRemovedEvent;
        public event RoomEvent RoomUpdatedEvent;
        public event UserEvent UserInvited;
        public event UserEvent UserRemoved;
        public event UserEvent UserEntered;
        public event UserEvent UserOut;
        public event MessageEvent MessageRecived;
        public event ImageEvent UserImage;
        public event MediaEvent UserVideo;
        public event MediaEvent UserSound;
        public event ErrorEvent Error;

        private IServerContract _server;
        private InstanceContext _instanceContext;

        public ClientService()
        {
            _instanceContext = new InstanceContext(this);
            ConfigLog();
        }

        private void ConfigLog()
        {
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

        protected void OnUserProfileUpdate(UserDto user, string name) => UserProfileUpdateEvent?.Invoke(user, name);
        protected void OnRoomRemove(RoomDto room) => RoomRemovedEvent?.Invoke(room);
        protected void OnRoomUpdate(RoomDto room) => RoomUpdatedEvent?.Invoke(room);
        protected void OnUserInvite(RoomDto room, UserDto user) => UserInvited?.Invoke(room, user);
        protected void OnUserRemove(RoomDto room, UserDto user) => UserRemoved?.Invoke(room, user);
        protected void OnUserEnter(RoomDto room, UserDto user) => UserEntered?.Invoke(room, user);
        protected void OnUserOut(RoomDto room, UserDto user) => UserOut?.Invoke(room, user);
        protected void OnMessageRecived(RoomDto room, MessageDto message) => MessageRecived?.Invoke(room, message);
        protected void OnChangeUserImage(ImageDto image, UserDto user) => UserImage?.Invoke(image, user);
        protected void SendUserVideo(RoomDto room, List<byte[]> video, UserDto user) => UserVideo?.Invoke(room, video, user);
        protected void PlayUserSound(RoomDto room, List<byte[]> arr, UserDto user) => UserSound?.Invoke(room, arr, user);
        protected void OnError(string error, bool reconnect, bool logout) => Error?.Invoke(error, reconnect, logout);

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
                clientChannelFactory = new DuplexChannelFactory<IServerContract>(_instanceContext, binding, endpoint);
                _server = clientChannelFactory.CreateChannel();
            }
            catch (Exception e)
            {
                ShowError("Проблемы при подключении к серверу.", true, true);
                Log.Error(e);
            }
        }

        public void Logout()
        {
            if (_server != null)
                _server.Logout();
        }


        public void UpdateProfileOnClientSide(UserDto user, string name)
        {
            OnUserProfileUpdate(user, name);
        }

        public void RoomRemoved(RoomDto room)
        {
            Log.Trace("Удаление комнаты {0}", room.Name);
            OnRoomRemove(room);
        }

        public void RoomUpdated(RoomDto room)
        {
            Log.Trace("Обновление комнаты {0}", room.Name);
            OnRoomUpdate(room);
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            Log.Trace("Добавление в комнату пользователя {0}", user.Name);
            OnUserInvite(room, user);
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            Log.Trace("Удаление из комнаты пользователя {0}", user.Name);
            OnUserRemove(room, user);
        }


        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            Log.Trace("Добавление онлайн пользователя {0}", user.Name);
            OnUserEnter(room, user);
        }


        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            Log.Trace("Удаление из онлайн пользователя {0}", user.Name);
            OnUserOut(room, user);
        }

        public void PrintMessage(RoomDto room, MessageDto message)
        {
            Log.Trace("Сообщение: {0}", message.MessageText);
            OnMessageRecived(room, message);
        }

        public void ChangeUserImage(UserDto user, ImageDto image)
        {
            Log.Trace("Изменение фото {0}", user.Name);
            OnChangeUserImage(image, user);
        }

        public void ShowVideo(RoomDto room, List<byte[]> video, UserDto user) => SendUserVideo(room, video, user);
        
        public void PlaySound(RoomDto room, List<byte[]> arr, UserDto user) => PlayUserSound(room, arr, user);

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
                return _server.CheckConnection();
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
                User = _server.Login(new LoginDto(name, password));
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
                User = _server.SignUp(new LoginDto(name, password));
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
        public bool UpdateProfile(string name, string password)
        {
            try
            {
                Log.Trace("Обновление профиля {0}", name);
                User = _server.UpdateProfile(new LoginDto(name, password));
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
        public void SendMessage(MessageDto message)
        {
            message.Owner = User;
            try
            {
                Log.Trace("Отправка сообщения");
                _server.SendMessage(message);
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
                User = _server.SetImage(image);
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

        public void AddRoomForVideo(RoomDto room)
        {
            try
            {
                _server.AddRoomForVideo(room);
            }
            catch (Exception ex)
            {

            }
        }
        public void RemoveRoomFromVideo(RoomDto room)
        {
            try
            {
                _server.RemoveRoomFromVideo(room);
            }
            catch(Exception ex)
            {

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
                    _server.SendVideo(room, SendingVideo);
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
        public void SendVoice(byte[] voice)
        {
            if (last_audio_sending == DateTime.MinValue) last_audio_sending = DateTime.Now;

            SendingAudio.Add(voice);
            if (DateTime.Now - last_audio_sending > TimeSpan.FromMilliseconds(20))
            {
                try
                {
                    _server.SendVoice(SendingAudio);
                }
                catch (CommunicationObjectFaultedException e)
                {
                    Log.Error(e);
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
                return _server.GetOnlineUsers(room);
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
                return _server.GetAvatar(name);
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
                return _server.GetAllUsers();
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
                return _server.GetUsers(room);
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
                return _server.IsOnline(room, name);
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
                return _server.LoadMessages(room, number, date);
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


        public int GetStreamCapacity() => _server.GetMaxFileLength();
        public void PrepareFile(long length) => _server.PrepareFile(length);
        public void SendPartOfFile(FilePartDto file) => _server.SendPartOfFile(file);

        public long GetFile(FileMessageDto message) => _server.GetFile(message);
        public byte[] DownloadFile(long recieved) => _server.DownloadFile(recieved);
        public void DeleteFile() => _server.DeleteFile();

        public List<RoomDto> GetRooms()
        {
            try
            {
                return _server.GetRooms();
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
                return _server.CreateRoom(room);
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
                _server.RemoveRoom(room);
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
                _server.UpdateRoom(room);
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
                _server.InviteUser(room, user);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }
        public void KickUser(RoomDto room, UserDto user)
        {
            try
            {
                _server.KickUser(room, user);
            }
            catch (Exception e)
            {
                Log.Error(e);
                ShowError("Проблемы при подключении к серверу", true, true);
            }
        }
    }

    public static class TaskExtensions
    {
        public static T WaitResult<T>(this Task<T> task, int timeout = 5000)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                task.Wait(50);
                if (task.IsCompleted) break;
            } while (sw.ElapsedMilliseconds < timeout);

            if (!task.IsCompleted)
            {
                throw new TimeoutException();
            }
            return task.Result;
        }

        public static void WaitCompletion(this Task task, int timeout = 5000)
        {
            var sw = Stopwatch.StartNew();
            do
            {
                task.Wait(50);
                if (task.IsCompleted) break;
            } while (sw.ElapsedMilliseconds < timeout);

            if (!task.IsCompleted)
            {
                throw new TimeoutException();
            }
        }
    }
}
