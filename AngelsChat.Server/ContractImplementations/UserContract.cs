using System.ServiceModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using System;
using AngelsChat.Shared.Data;
using AngelsChat.Server.Data.Entities;
using AngelsChat.Server.Core;
using Microsoft.AspNet.SignalR;
using System.Linq;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Reflection;

namespace AngelsChat.Server.ContractImplementations
{
    public static class TypeExtensions
    {
        public static object GetDefault(this Type type)
        {
            if (type == typeof(void))
            {
                return null;
            }
            else if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }
    }
    public class UnionSubscribers<T> : RealProxy where T : class
    {
        private readonly Dictionary<object, object> _dictionary;
        private readonly object _syncRoot;

        internal UnionSubscribers(object syncRoot, Dictionary<object, object> dictionary)
             : base(typeof(T))
        {
            _dictionary = dictionary;
            _syncRoot = syncRoot;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            var name = methodCall.MethodName;

            lock (_syncRoot)
            {
                foreach (var target in Cast())
                {
                    methodInfo.Invoke(target, methodCall.Args);
                }
            }
            var result = methodInfo.ReturnType.GetDefault();

            return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        private IEnumerable<T> Cast()
        {
            return _dictionary.Values.OfType<T>();
        }
    }
    public class MethodTask
    {
        private object _sender;
        private readonly MethodInfo _method;
        private readonly object[] _arguments;

        public void Invoke()
        {
            _method.Invoke(_sender, _arguments);
        }

        public MethodTask(object sender, MethodInfo method, params object[] arguments)
        {
            _sender = sender;
            _method = method;
            _arguments = arguments;
        }
    }
    public class ShrinkList<T>
    {
        private List<T> _tasks = new List<T>();
        private object _tasksLocker = new object();
        private bool _processing;
        private bool _wait = false;
        private Task _task;

        public Action<List<T>> Handler { get; set; }

        public bool WaitUntilTheEnd
        {
            get => _wait;
            set
            {
                if (_wait == value) return;
                _wait = value;
                if (_wait) AppDomain.CurrentDomain.ProcessExit += TaskWaiter;
                else AppDomain.CurrentDomain.ProcessExit -= TaskWaiter;
            }
        }

        private void TaskWaiter(object sender, EventArgs e)
        {
            if (_wait) _task?.Wait();
        }

        public void Add(T unit)
        {
            lock (_tasksLocker)
                if (_processing) _tasks.Add(unit);
                else
                {
                    _processing = true;
                    _task = Task.Factory.StartNew(() => TaskProcess(unit));
                }
        }

        private void OnProcess(List<T> units)
        {
            if (Handler != null)
                try { Handler(units); }
                catch { }
        }

        private void TaskProcess(T unit)
        {
            var units = new List<T> { unit };
            do
            {
                OnProcess(units);

                lock (_tasksLocker)
                    if (_tasks.Count > 0)
                    {
                        units = _tasks.ToList();
                        _tasks.Clear();
                    }
                    else
                    {
                        _processing = false;
                        return;
                    }
                Task.Delay(1).Wait();
            } while (true);
        }
    }
    public class Subscriber<T> : RealProxy where T : class
    {
        public static T SubscriberFrom(object target)
        {
            var result = new Subscriber<T>(target).GetTransparentProxy();
            return (T)result;
        }

        private readonly Dictionary<string, ShrinkList<MethodTask>> _subscriptions =
            new Dictionary<string, ShrinkList<MethodTask>>();
        private readonly object _target;

        public event Action<Exception> OnError;

        public Subscriber(object target) : base(typeof(T))
        {
            _target = target;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;
            var name = methodCall.MethodName;

            object result = null;

            // todo: временное решение:
            // Проверка реализует ли _target требуемый метод
            if (!methodInfo.ReflectedType.IsAssignableFrom(_target.GetType()))
            {
                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }

            if (!_subscriptions.ContainsKey(name))
            {
                _subscriptions[name] = CreateTaskQueue();
            }
            if (methodInfo.ReturnType == typeof(void))
            {
                _subscriptions[name]
                    .Add(new MethodTask(_target, methodInfo, methodCall.Args));
            }
            else
            {
                try
                {
                    result = methodInfo.Invoke(_target, methodCall.Args);
                }
                catch
                {
                    result = methodInfo.ReturnType.GetDefault();
                }
            }

            return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
        }

        private ShrinkList<MethodTask> CreateTaskQueue()
        {
            var queue = new ShrinkList<MethodTask>
            {
                Handler = Handle
            };
            return queue;
        }

        private void Handle(List<MethodTask> tasks)
        {
            try
            {
                tasks.LastOrDefault()?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }
    }
    public class SubscriptionManager
    {
        private readonly Dictionary<object, object> _dictionary = new Dictionary<object, object>();
        private readonly object _syncRoot = new object();

        public bool IsEmpty()
        {
            lock (_syncRoot)
            {
                return _dictionary.Count == 0;
            }
        }

        public bool HasKey(object key)
        {
            lock (_syncRoot)
            {
                return _dictionary.ContainsKey(key);
            }
        }

        public bool HasSubscribers<T>()
        {
            lock (_syncRoot)
            {
                return _dictionary.Values.OfType<T>().Any();
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _dictionary.Clear();
            }
        }

        public void AddSubscriber<T>(object key, object target) where T : class
        {
            lock (_syncRoot)
            {
                _dictionary[key] = Subscriber<T>.SubscriberFrom(target);
            }
        }

        public void RemoveSubscriber(object key)
        {
            lock (_syncRoot)
            {
                _dictionary.Remove(key);
            }
        }

        public T ForEach<T>() where T : class
        {
            var result = new UnionSubscribers<T>(_syncRoot, _dictionary).GetTransparentProxy();
            return (T)result;
        }

        public T GetSubscriber<T>(object key) where T : class
        {
            var contains = _dictionary.ContainsKey(key);
            return contains ? _dictionary[key] as T : null;
        }
    }

    public interface ISubscribe
    {
        bool IsSubscribed(SignalRHubClient client);
        void Subscribe(SignalRHubClient client);
        void Unsubscribe(SignalRHubClient client);
    }

    public interface IMainServerContract
    {
        SignalRHubClient ServerContract { get; set; }

        void AddRoomForVideo(RoomDto room, SignalRHubClient userContract);
        RoomDto CreateRoom(RoomDto room, SignalRHubClient userContract);
        List<UserDto> GetAllUsers();
        ImageDto GetAvatar(string name);
        byte[] GetFile(SignalRHubClient userContract, FileMessageDto file);
        List<UserDto> GetOnlineUsers(RoomDto room);
        RoomManager GetRoom(RoomDto room);
        List<RoomDto> GetRooms(SignalRHubClient userContract);
        List<UserDto> GetUsers(RoomDto room);
        void InviteUser(RoomDto room, UserDto person, SignalRHubClient userContract);
        bool IsOnline(RoomDto room, string name);
        void KickUser(RoomDto room, UserDto person, SignalRHubClient userContract);
        List<MessageDto> LoadMessages(RoomDto room, SignalRHubClient userContract, int number, DateTime? date);
        UserDto Login(SignalRHubClient userContract, LoginDto login);
        void Logout(SignalRHubClient contract);
        UserDto Registration(SignalRHubClient userContract, LoginDto login);
        void RemoveRoom(RoomDto room, SignalRHubClient userContract);
        void RemoveRoomFromVideo(RoomDto room, SignalRHubClient userContract);
        void SendMessage(SignalRHubClient userContract, MessageDto message);
        void SendVideo(RoomDto room, SignalRHubClient userContract, List<byte[]> video);
        void SendVoice(SignalRHubClient userContract, List<byte[]> voice);
        void SetAvatar(SignalRHubClient userContract, ImageDto image);
        UserDto UpdateProfile(SignalRHubClient userContract, LoginDto login);
        void UpdateRoom(RoomDto room, SignalRHubClient userContract);
        void UserEnter(SignalRHubClient userContract);
        void UserLeave(SignalRHubClient userContract);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Contract : IMainServerContract, ISubscribe
    {
        private readonly SubscriptionManager _subscriptionManager = new SubscriptionManager();

        #region static
        private static Contract _current;
        public static Contract Current => CreateInstance();

        public SignalRHubClient ServerContract { get => ((IMainServerContract)_current).ServerContract; set => ((IMainServerContract)_current).ServerContract = value; }

        public static Contract CreateInstance()
        {
            if (_current == null)
            {
                _current = new Contract();
            }
            return _current;
        }
        #endregion

        #region ISubscribe

        public bool IsSubscribed(SignalRHubClient client)
        {
            return _subscriptionManager.HasKey(client);
        }

        public void Subscribe(SignalRHubClient client)
        {
            _subscriptionManager.AddSubscriber<SignalRHubClient>(client, client._systemHub);
        }

        public void Unsubscribe(SignalRHubClient client)
        {
            _subscriptionManager.RemoveSubscriber(client);
        }

        #endregion

        #region IMainServerContract

        public void AddRoomForVideo(RoomDto room, SignalRHubClient userContract)
        {
            MainServer.Current.AddRoomForVideo(room, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public RoomDto CreateRoom(RoomDto room, SignalRHubClient userContract)
        {
            return MainServer.Current.CreateRoom(room, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public List<UserDto> GetAllUsers()
        {
            return MainServer.Current.GetAllUsers();
        }

        public ImageDto GetAvatar(string name)
        {
            return MainServer.Current.GetAvatar(name);
        }

        public byte[] GetFile(SignalRHubClient userContract, FileMessageDto file)
        {
            return MainServer.Current.GetFile(_subscriptionManager.GetSubscriber<IContract>(userContract), file);
        }

        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            return MainServer.Current.GetOnlineUsers(room);
        }

        public RoomManager GetRoom(RoomDto room)
        {
            return MainServer.Current.GetRoom(room);
        }

        public List<RoomDto> GetRooms(SignalRHubClient userContract)
        {
            return MainServer.Current.GetRooms(_subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public List<UserDto> GetUsers(RoomDto room)
        {
            return MainServer.Current.GetUsers(room);
        }

        public void InviteUser(RoomDto room, UserDto person, SignalRHubClient userContract)
        {
            MainServer.Current.InviteUser(room, person, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public bool IsOnline(RoomDto room, string name)
        {
            return MainServer.Current.IsOnline(room, name);
        }

        public void KickUser(RoomDto room, UserDto person, SignalRHubClient userContract)
        {
            MainServer.Current.KickUser(room, person, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public List<MessageDto> LoadMessages(RoomDto room, SignalRHubClient userContract, int number, DateTime? date)
        {
            return MainServer.Current.LoadMessages(room, _subscriptionManager.GetSubscriber<IContract>(userContract), number, date);
        }

        public UserDto Login(SignalRHubClient userContract, LoginDto login)
        {
            return MainServer.Current.Login(_subscriptionManager.GetSubscriber<IContract>(userContract), login);
        }

        public void Logout(SignalRHubClient userContract)
        {
            MainServer.Current.Logout(_subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public UserDto Registration(SignalRHubClient userContract, LoginDto login)
        {
            return MainServer.Current.Registration(_subscriptionManager.GetSubscriber<IContract>(userContract), login);
        }

        public void RemoveRoom(RoomDto room, SignalRHubClient userContract)
        {
            MainServer.Current.RemoveRoom(room, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public void RemoveRoomFromVideo(RoomDto room, SignalRHubClient userContract)
        {
            MainServer.Current.RemoveRoomFromVideo(room, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public void SendMessage(SignalRHubClient userContract, MessageDto message)
        {
            MainServer.Current.SendMessage(_subscriptionManager.GetSubscriber<IContract>(userContract), message);
        }

        public void SendVideo(RoomDto room, SignalRHubClient userContract, List<byte[]> video)
        {
            MainServer.Current.SendVideo(room, _subscriptionManager.GetSubscriber<IContract>(userContract), video);
        }

        public void SendVoice(SignalRHubClient userContract, List<byte[]> voice)
        {
            MainServer.Current.SendVoice(_subscriptionManager.GetSubscriber<IContract>(userContract), voice);
        }

        public void SetAvatar(SignalRHubClient userContract, ImageDto image)
        {
            MainServer.Current.SetAvatar(_subscriptionManager.GetSubscriber<IContract>(userContract), image);
        }

        public UserDto UpdateProfile(SignalRHubClient userContract, LoginDto login)
        {
            return MainServer.Current.UpdateProfile(_subscriptionManager.GetSubscriber<IContract>(userContract), login);
        }

        public void UpdateRoom(RoomDto room, SignalRHubClient userContract)
        {
            MainServer.Current.UpdateRoom(room, _subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        public void UserEnter(SignalRHubClient userContract)
        {
            var subscriber = _subscriptionManager.GetSubscriber<SignalRHubClient>(userContract);
            MainServer.Current.UserEnter(subscriber._systemHub);
        }

        public void UserLeave(SignalRHubClient userContract)
        {
            MainServer.Current.UserLeave(_subscriptionManager.GetSubscriber<IContract>(userContract));
        }

        #endregion
    }

    //public class SignalRHubClientRemoteable : MarshalByRefObject, IContract
    //{
    //    private IContract _instance;
    //    public SignalRHubClientRemoteable(IContract instance)
    //    {
    //        _instance = instance;
    //    }
    //    #region interface
    //    public User User { get => _instance.User; set => _instance.User = value; }

    //    public void AddOnlineUser(RoomDto room, UserDto user)
    //    {
    //        _instance.AddOnlineUser(room, user);
    //    }

    //    public void AddRoomForVideo(RoomDto room)
    //    {
    //        _instance.AddRoomForVideo(room);
    //    }

    //    public void AddUser(RoomDto room, UserDto user)
    //    {
    //        _instance.AddUser(room, user);
    //    }

    //    public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
    //    {
    //        _instance.ChangeUserImage(room, user, image);
    //    }

    //    public bool CheckConnection()
    //    {
    //        return _instance.CheckConnection();
    //    }

    //    public void ClearUserFile()
    //    {
    //        _instance.ClearUserFile();
    //    }

    //    public RoomDto CreateRoom(RoomDto room)
    //    {
    //        return _instance.CreateRoom(room);
    //    }

    //    public void DeleteFile()
    //    {
    //        _instance.DeleteFile();
    //    }

    //    public byte[] DownloadFile(long recieved)
    //    {
    //        return _instance.DownloadFile(recieved);
    //    }

    //    public List<UserDto> GetAllUsers()
    //    {
    //        return _instance.GetAllUsers();
    //    }

    //    public ImageDto GetAvatar(string name)
    //    {
    //        return _instance.GetAvatar(name);
    //    }

    //    public long GetFile(FileMessageDto file)
    //    {
    //        return _instance.GetFile(file);
    //    }

    //    public int GetMaxFileLength()
    //    {
    //        return _instance.GetMaxFileLength();
    //    }

    //    public List<UserDto> GetOnlineUsers(RoomDto room)
    //    {
    //        return _instance.GetOnlineUsers(room);
    //    }

    //    public List<RoomDto> GetRooms()
    //    {
    //        return _instance.GetRooms();
    //    }

    //    public void GetStarted()
    //    {
    //        _instance.GetStarted();
    //    }

    //    public byte[] GetUserFile()
    //    {
    //        return _instance.GetUserFile();
    //    }

    //    public List<UserDto> GetUsers(RoomDto room)
    //    {
    //        return _instance.GetUsers(room);
    //    }

    //    public void InviteUser(RoomDto room, UserDto user)
    //    {
    //        _instance.InviteUser(room, user);
    //    }

    //    public bool IsOnline(RoomDto room, string name)
    //    {
    //        return _instance.IsOnline(room, name);
    //    }

    //    public void KickUser(RoomDto room, UserDto user)
    //    {
    //        _instance.KickUser(room, user);
    //    }

    //    public List<MessageDto> LoadMessages(RoomDto room, int number, DateTime? date)
    //    {
    //        return _instance.LoadMessages(room, number, date);
    //    }

    //    public UserDto Login(LoginDto login)
    //    {
    //        return _instance.Login(login);
    //    }

    //    public void Logout()
    //    {
    //        _instance.Logout();
    //    }

    //    public void PrepareFile(long length)
    //    {
    //        _instance.PrepareFile(length);
    //    }

    //    public void RemoveOnlineUser(RoomDto room, UserDto user)
    //    {
    //        _instance.RemoveOnlineUser(room, user);
    //    }

    //    public void RemoveRoom(RoomDto room)
    //    {
    //        _instance.RemoveRoom(room);
    //    }

    //    public void RemoveRoomFromVideo(RoomDto room)
    //    {
    //        _instance.RemoveRoomFromVideo(room);
    //    }

    //    public void RemoveUser(RoomDto room, UserDto user)
    //    {
    //        _instance.RemoveUser(room, user);
    //    }

    //    public void RoomRemoved(RoomDto room)
    //    {
    //        _instance.RoomRemoved(room);
    //    }

    //    public void RoomUpdated(RoomDto room)
    //    {
    //        _instance.RoomUpdated(room);
    //    }

    //    public void SendMessage(MessageDto message)
    //    {
    //        _instance.SendMessage(message);
    //    }

    //    public void SendPartOfFile(FilePartDto file)
    //    {
    //        _instance.SendPartOfFile(file);
    //    }

    //    public void SendReply(RoomDto room, MessageDto message)
    //    {
    //        _instance.SendReply(room, message);
    //    }

    //    public void SendVideo(RoomDto room, List<byte[]> video)
    //    {
    //        _instance.SendVideo(room, video);
    //    }

    //    public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
    //    {
    //        _instance.SendVideoForUser(room, video, user);
    //    }

    //    public void SendVoice(List<byte[]> voice)
    //    {
    //        _instance.SendVoice(voice);
    //    }

    //    public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
    //    {
    //        _instance.SendVoiceForUser(room, voice, user);
    //    }

    //    public UserDto SetImage(ImageDto image)
    //    {
    //        return _instance.SetImage(image);
    //    }

    //    public UserDto SignUp(LoginDto login)
    //    {
    //        return _instance.SignUp(login);
    //    }

    //    public UserDto UpdateProfile(LoginDto login)
    //    {
    //        return _instance.UpdateProfile(login);
    //    }

    //    public void UpdateProfileOnClientSide(UserDto uer, string name)
    //    {
    //        _instance.UpdateProfileOnClientSide(uer, name);
    //    }

    //    public void UpdateRoom(RoomDto room)
    //    {
    //        _instance.UpdateRoom(room);
    //    }
    //    #endregion
    //}

    public class SignalRHubClient : MarshalByRefObject
    {
        public readonly string _connectionId;
        public readonly SignalRHub _systemHub;

        public SignalRHubClient(string connectionId, SignalRHub systemHub)
        {
            _connectionId = connectionId;
            _systemHub = systemHub;
        }

        public SignalRHubClient(string connectionId)
        {
            _connectionId = connectionId;
        }

        public override int GetHashCode()
        {
            return _connectionId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals((obj as SignalRHubClient)._connectionId, _connectionId);
        }

        public bool Equals(SignalRHubClient other)
        {
            return _connectionId == other?._connectionId;
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SignalRHub : Hub, IContract
    {
        private readonly IMainServerContract _system;
        private dynamic _callback;

        private SignalRHubClient CurrentClient
        {
            get => new SignalRHubClient(Context.ConnectionId, this);
        }

        public SignalRHub(IMainServerContract system)
        {
            _system = system;
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _system.Logout(CurrentClient);
            return base.OnDisconnected(stopCalled);
        }

        #region implementation IContract

        private bool authorized => Contract.Current.IsSubscribed(CurrentClient); //TODO
        public User User { get; set; }
        /// <summary>
        /// Start work when logged
        /// </summary>
        public void GetStarted()
        {
            Contract.Current.Subscribe(CurrentClient);
            Contract.Current.UserEnter(CurrentClient);
            _callback = Clients.Client(Context.ConnectionId);
            //OperationContext.Current.InstanceContext.Closing += (e, a) => MainServer.Current.UserLeave(this);
        }

        /// <summary>
        /// Message for user
        /// </summary>
        public void SendReply(RoomDto room, MessageDto message)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _callback.PrintMessage(room, message);
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        /// <summary>
        /// Message from user
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(MessageDto message)
        {
            if (authorized)
            {
                MainServer.Current.SendMessage(this, message);
            }
            else { }
        }

        /// <summary>
        /// Change user image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public UserDto SetImage(ImageDto image)
        {
            if (authorized)
            {
                Task.Factory.StartNew(() => MainServer.Current.SetAvatar(this, image));
                return User.ToUserDto(User);
            }
            else
            {
                return null;
            }
        }

        public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _callback.ChangeUserImage(user, image);
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public UserDto UpdateProfile(LoginDto login)
        {
            var request = MainServer.Current.UpdateProfile(this, login);
            return request;
        }
        /// <summary>
        /// Authorization
        /// </summary>
        public UserDto Login(LoginDto login)
        {
            var request = MainServer.Current.Login(this, login);
            if (request != null)
            {
                GetStarted();
            }
            else
            {
            }
            return request;
        }

        /// <summary>
        /// Registration
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public UserDto SignUp(LoginDto login)
        {
            var request = MainServer.Current.Registration(this, login);
            if (request != null)
            {
                GetStarted();
            }
            else
            {
            }
            return request;
        }

        public bool CheckConnection()
        {
            return true;
        }

        /// <summary>
        /// Get online users
        /// </summary>
        /// <returns></returns>
        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            var task = Task.Factory.StartNew(() => MainServer.Current.GetOnlineUsers(room));
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Video stream
        /// </summary>
        /// <param name="video"></param>
        public void SendVideo(RoomDto room, List<byte[]> video)
        {
            if (authorized)
                try
                {
                    MainServer.Current.SendVideo(room, this, video);
                }
                catch (Exception e)
                {
                }

        }

        public void AddRoomForVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.AddRoomForVideo(room, this);
                }
                catch (Exception e)
                {
                }

        }

        public void RemoveRoomFromVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoomFromVideo(room, this);
                }
                catch (Exception e)
                {
                }

        }
        /// <summary>
        /// Video for user
        /// </summary>
        /// <param name="video"></param>
        /// <param name="user"></param>
        public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _callback.ShowVideo(room, video, user);
                    }
                    catch (System.Exception e)
                    {
                    }
                });
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Factory.StartNew(() => { _callback.AddUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                    }
                });
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Factory.StartNew(() => _callback.RemoveUser(room, user));
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Factory.StartNew(() => { _callback.AddOnlineUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                    }
                });
        }
        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Factory.StartNew(() => _callback.RemoveOnlineUser(room, user));
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public void RoomRemoved(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Factory.StartNew(() => _callback.RoomRemoved(room));
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public void RoomUpdated(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Task.Factory.StartNew(() => _callback.RoomUpdated(room));
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public void UpdateProfileOnClientSide(UserDto user, string name)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _callback.UpdateProfileOnClientSide(user, name);
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public List<MessageDto> LoadMessages(RoomDto room, int number, System.DateTime? date)
        {
            var task = Task.Factory.StartNew(() => Contract.Current.LoadMessages(room, CurrentClient, number, date));
            task.Wait();
            return task.Result;
        }

        public ImageDto GetAvatar(string name)
        {
            return Contract.Current.GetAvatar(name);
        }

        public List<UserDto> GetUsers(RoomDto room)
        {
            return Contract.Current.GetUsers(room);
        }

        public List<UserDto> GetAllUsers()
        {
            return Contract.Current.GetAllUsers();
        }

        public bool IsOnline(RoomDto room, string name)
        {
            return Contract.Current.IsOnline(room, name);
        }

        public int GetMaxFileLength()
        {
            var task = Task.Factory.StartNew(() => 1024);
            task.Wait();
            return task.Result;
        }

        private byte[] userFile;
        public byte[] GetUserFile() => userFile;
        public void ClearUserFile() => Array.Clear(userFile, 0, userFile.Length);

        public void PrepareFile(long length) => userFile = new byte[length];
        public void SendPartOfFile(FilePartDto file) => Task.Factory.StartNew(() => Array.Copy(file.Buffer, 0, userFile, file.StartIndex, file.Length));

        byte[] serverFile;
        public long GetFile(FileMessageDto file)
        {
            var task = Task.Factory.StartNew(() => Contract.Current.GetFile(CurrentClient, file));
            task.Wait();
            serverFile = task.Result;
            return serverFile.Length;
        }
        public byte[] DownloadFile(long recieved)
        {
            long length = Math.Min(GetMaxFileLength(), serverFile.Length - recieved);
            byte[] partOfFile = new byte[length];
            Array.Copy(serverFile, recieved, partOfFile, 0, length);
            return partOfFile;
        }
        public void DeleteFile()
        {
            serverFile = null;
        }

        public void Logout()
        {
            Contract.Current.Logout(CurrentClient);
        }

        public void SendVoice(List<byte[]> voice)
        {
            if (authorized)
                try
                {
                    Contract.Current.SendVoice(CurrentClient, voice);
                }
                catch (Exception e)
                {
                }
        }

        public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        {
            if (authorized && user.Name != User.Name)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        _callback.PlaySound(room, voice, user);
                    }
                    catch (Exception e)
                    {
                    }
                });
        }

        public List<RoomDto> GetRooms()
        {
            if (authorized)
                try
                {
                    return Contract.Current.GetRooms(CurrentClient);
                }
                catch (Exception e)
                {
                }
            return null;
        }

        public RoomDto CreateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    return Contract.Current.CreateRoom(room, CurrentClient);
                }
                catch (Exception e)
                {
                }
            return null;
        }

        public void RemoveRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    Contract.Current.RemoveRoom(room, CurrentClient);
                }
                catch (Exception e)
                {
                }
        }

        public void UpdateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    Contract.Current.UpdateRoom(room, CurrentClient);
                }
                catch (Exception e)
                {
                }
        }

        public void InviteUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    Contract.Current.InviteUser(room, user, CurrentClient);
                }
                catch (Exception e)
                {
                }
        }
        public void KickUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    Contract.Current.KickUser(room, user, CurrentClient);
                }
                catch (Exception e)
                {
                }
        }
        #endregion

        //#region interface IContract
        //public User User { get => _system.User; set => _system.User = value; }

        //public void AddOnlineUser(RoomDto room, UserDto user)
        //{
        //    _system.AddOnlineUser(room, user);
        //}

        //public void AddRoomForVideo(RoomDto room)
        //{
        //    _system.AddRoomForVideo(room);
        //}

        //public void AddUser(RoomDto room, UserDto user)
        //{
        //    _system.AddUser(room, user);
        //}

        //public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
        //{
        //    _system.ChangeUserImage(room, user, image);
        //}

        //public bool CheckConnection()
        //{
        //    return _system.CheckConnection();
        //}

        //public void ClearUserFile()
        //{
        //    _system.ClearUserFile();
        //}

        //public RoomDto CreateRoom(RoomDto room)
        //{
        //    return _system.CreateRoom(room);
        //}

        //public void DeleteFile()
        //{
        //    _system.DeleteFile();
        //}

        //public byte[] DownloadFile(long recieved)
        //{
        //    return _system.DownloadFile(recieved);
        //}

        //public List<UserDto> GetAllUsers()
        //{
        //    return _system.GetAllUsers();
        //}

        //public ImageDto GetAvatar(string name)
        //{
        //    return _system.GetAvatar(name);
        //}

        //public long GetFile(FileMessageDto file)
        //{
        //    return _system.GetFile(file);
        //}

        //public int GetMaxFileLength()
        //{
        //    return _system.GetMaxFileLength();
        //}

        //public List<UserDto> GetOnlineUsers(RoomDto room)
        //{
        //    return _system.GetOnlineUsers(room);
        //}

        //public List<RoomDto> GetRooms()
        //{
        //    return _system.GetRooms();
        //}

        //public void GetStarted()
        //{
        //    _system.GetStarted();
        //}

        //public byte[] GetUserFile()
        //{
        //    return _system.GetUserFile();
        //}

        //public List<UserDto> GetUsers(RoomDto room)
        //{
        //    return _system.GetUsers(room);
        //}

        //public void InviteUser(RoomDto room, UserDto user)
        //{
        //    _system.InviteUser(room, user);
        //}

        //public bool IsOnline(RoomDto room, string name)
        //{
        //    return _system.IsOnline(room, name);
        //}

        //public void KickUser(RoomDto room, UserDto user)
        //{
        //    _system.KickUser(room, user);
        //}

        //public List<MessageDto> LoadMessages(RoomDto room, int number, DateTime? date)
        //{
        //    return _system.LoadMessages(room, number, date);
        //}

        //public UserDto Login(LoginDto login)
        //{
        //    return _system.Login(login);
        //}

        //public void Logout()
        //{
        //    _system.Logout();
        //}

        //public void PrepareFile(long length)
        //{
        //    _system.PrepareFile(length);
        //}

        //public void RemoveOnlineUser(RoomDto room, UserDto user)
        //{
        //    _system.RemoveOnlineUser(room, user);
        //}

        //public void RemoveRoom(RoomDto room)
        //{
        //    _system.RemoveRoom(room);
        //}

        //public void RemoveRoomFromVideo(RoomDto room)
        //{
        //    _system.RemoveRoomFromVideo(room);
        //}

        //public void RemoveUser(RoomDto room, UserDto user)
        //{
        //    _system.RemoveUser(room, user);
        //}

        //public void RoomRemoved(RoomDto room)
        //{
        //    _system.RoomRemoved(room);
        //}

        //public void RoomUpdated(RoomDto room)
        //{
        //    _system.RoomUpdated(room);
        //}

        //public void SendMessage(MessageDto message)
        //{
        //    _system.SendMessage(message);
        //}

        //public void SendPartOfFile(FilePartDto file)
        //{
        //    _system.SendPartOfFile(file);
        //}

        //public void SendReply(RoomDto room, MessageDto message)
        //{
        //    _system.SendReply(room, message);
        //}

        //public void SendVideo(RoomDto room, List<byte[]> video)
        //{
        //    _system.SendVideo(room, video);
        //}

        //public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        //{
        //    _system.SendVideoForUser(room, video, user);
        //}

        //public void SendVoice(List<byte[]> voice)
        //{
        //    _system.SendVoice(voice);
        //}

        //public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        //{
        //    _system.SendVoiceForUser(room, voice, user);
        //}

        //public UserDto SetImage(ImageDto image)
        //{
        //    return _system.SetImage(image);
        //}

        //public UserDto SignUp(LoginDto login)
        //{
        //    return _system.SignUp(login);
        //}

        //public UserDto UpdateProfile(LoginDto login)
        //{
        //    return _system.UpdateProfile(login);
        //}

        //public void UpdateProfileOnClientSide(UserDto uer, string name)
        //{
        //    _system.UpdateProfileOnClientSide(uer, name);
        //}

        //public void UpdateRoom(RoomDto room)
        //{
        //    _system.UpdateRoom(room);
        //}

        //#endregion 
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class HubUserContract : Hub, IContract
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static HubUserContract Current { get; } = new HubUserContract();

        public HubUserContract() { }

        private dynamic callback => Clients.Client(Context.ConnectionId);

        private Dictionary<string, User> _users = new Dictionary<string, User>();
        public User User { get => _users[Context.ConnectionId]; set => _users[Context.ConnectionId] = value; }

        private bool authorized => IsAuthorized();
        private bool IsAuthorized()
        {
            //if (!_usersAuthorization.ContainsKey(Context.ConnectionId))
            //    _usersAuthorization[Context.ConnectionId] = false;
            return _usersAuthorization[Context.ConnectionId];
        }
        private Dictionary<string, bool> _usersAuthorization = new Dictionary<string, bool>();

        /// <summary>
        /// Start work when logged
        /// </summary>
        public void GetStarted()
        {
            Log.Info("Инициализация UserContract");
            MainServer.Current.UserEnter(this);
            _usersAuthorization[Context.ConnectionId] = true;
            var a = OperationContext.Current.InstanceContext;
            //OperationContext.Current.InstanceContext.Closing += (e, a) => MainServer.Current.UserLeave(this);
        }

        public override Task OnConnected()
        {
            _usersAuthorization[Context.ConnectionId] = false;
            _users[Context.ConnectionId] = null;
            return base.OnConnected();
        }

        /// <summary>
        /// Message for user
        /// </summary>
        public void SendReply(RoomDto room, MessageDto message)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Отправка сообщения пользователю");
                        callback.PrintMessage(room, message);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        /// <summary>
        /// Message from user
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(MessageDto message)
        {
            if (authorized)
            {
                Log.Trace("Отправка сообщения на сервер");
                MainServer.Current.SendMessage(this, message);
            }
            else
                Log.Trace("Отправка сообщения не удалась, пользователь не авторизован");
        }

        /// <summary>
        /// Change user image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public UserDto SetImage(ImageDto image)
        {
            if (authorized)
            {
                Log.Trace("Отправка изображения на сервер");
                Task.Factory.StartNew(() => MainServer.Current.SetAvatar(this, image));
                return User.ToUserDto(User);
            }
            else
            {
                Log.Trace("Установка изображения не удалась, пользователь не авторизован");
                return null;
            }
        }

        public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Изменить изображение");
                        callback.ChangeUserImage(user, image);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public UserDto UpdateProfile(LoginDto login)
        {
            var request = MainServer.Current.UpdateProfile(this, login);
            return request;
        }
        /// <summary>
        /// Authorization
        /// </summary>
        public UserDto Login(LoginDto login)
        {
            var request = MainServer.Current.Login(this, login);
            if (request != null)
            {
                GetStarted();
                Log.Info("Авторизация успешна, {0}", login.Name);
            }
            else
            {
                Log.Info("Авторизация не удалась, {0}", login.Name);
            }
            return request;
        }

        /// <summary>
        /// Registration
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public UserDto SignUp(LoginDto login)
        {
            var request = MainServer.Current.Registration(this, login);
            if (request != null)
            {
                GetStarted();
                Log.Info("Регистрация успешна, {0}", login.Name);
            }
            else
            {
                Log.Info("Регистрация не удалась, {0}", login.Name);
            }
            return request;
        }

        public bool CheckConnection()
        {
            Log.Debug("Соединение успешно");
            return true;
        }

        /// <summary>
        /// Get online users
        /// </summary>
        /// <returns></returns>
        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            Log.Trace("Получение пользователей");
            var task = Task.Factory.StartNew(() => MainServer.Current.GetOnlineUsers(room));
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Video stream
        /// </summary>
        /// <param name="video"></param>
        public void SendVideo(RoomDto room, List<byte[]> video)
        {
            if (authorized)
                try
                {
                    MainServer.Current.SendVideo(room, this, video);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }

        public void AddRoomForVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.AddRoomForVideo(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }

        public void RemoveRoomFromVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoomFromVideo(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }
        /// <summary>
        /// Video for user
        /// </summary>
        /// <param name="video"></param>
        /// <param name="user"></param>
        public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.ShowVideo(room, video, user);
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        Task.Factory.StartNew(() => callback.RemoveUser(room, user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddOnlineUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }
        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        Task.Factory.StartNew(() => callback.RemoveOnlineUser(room, user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RoomRemoved(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", room.Name);
                        Task.Factory.StartNew(() => callback.RoomRemoved(room));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RoomUpdated(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", room.Name);
                        Task.Factory.StartNew(() => callback.RoomUpdated(room));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void UpdateProfileOnClientSide(UserDto user, string name)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.UpdateProfileOnClientSide(user, name);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<MessageDto> LoadMessages(RoomDto room, int number, System.DateTime? date)
        {
            Log.Trace("Загрузка сообщений");
            var task = Task.Factory.StartNew(() => MainServer.Current.LoadMessages(room, this, number, date));
            task.Wait();
            return task.Result;
        }

        public ImageDto GetAvatar(string name)
        {
            Log.Trace("Загрузка аватарки {0}", name);
            return MainServer.Current.GetAvatar(name);
        }

        public List<UserDto> GetUsers(RoomDto room)
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetUsers(room);
        }

        public List<UserDto> GetAllUsers()
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetAllUsers();
        }

        public bool IsOnline(RoomDto room, string name)
        {
            Log.Trace("{0} онлайн?", name);
            return MainServer.Current.IsOnline(room, name);
        }

        public int GetMaxFileLength()
        {
            var task = Task.Factory.StartNew(() => 1024);
            task.Wait();
            return task.Result;
        }

        private byte[] userFile;
        public byte[] GetUserFile() => userFile;
        public void ClearUserFile() => Array.Clear(userFile, 0, userFile.Length);

        public void PrepareFile(long length) => userFile = new byte[length];
        public void SendPartOfFile(FilePartDto file) => Task.Factory.StartNew(() => Array.Copy(file.Buffer, 0, userFile, file.StartIndex, file.Length));

        byte[] serverFile;
        public long GetFile(FileMessageDto file)
        {
            var task = Task.Factory.StartNew(() => MainServer.Current.GetFile(this, file));
            task.Wait();
            serverFile = task.Result;
            return serverFile.Length;
        }
        public byte[] DownloadFile(long recieved)
        {
            long length = Math.Min(GetMaxFileLength(), serverFile.Length - recieved);
            byte[] partOfFile = new byte[length];
            Array.Copy(serverFile, recieved, partOfFile, 0, length);
            return partOfFile;
        }
        public void DeleteFile()
        {
            serverFile = null;
        }

        public void Logout()
        {
            MainServer.Current.Logout(this);
        }

        public void SendVoice(List<byte[]> voice)
        {
            if (authorized)
                try
                {
                    MainServer.Current.SendVoice(this, voice);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        {
            if (authorized && user.Name != User.Name)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.PlaySound(room, voice, user);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<RoomDto> GetRooms()
        {
            if (authorized)
                try
                {
                    return MainServer.Current.GetRooms(this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            return null;
        }

        public RoomDto CreateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    return MainServer.Current.CreateRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            return null;
        }

        public void RemoveRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void UpdateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.UpdateRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void InviteUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    MainServer.Current.InviteUser(room, user, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }
        public void KickUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    MainServer.Current.KickUser(room, user, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }
    }


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class UserContract : Hub, IContract
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public UserContract() {}

        private dynamic callback;
        public User User { get; set; }
        private bool authorized = false;

        /// <summary>
        /// Start work when logged
        /// </summary>
        public void GetStarted()
        {
            Log.Info("Инициализация UserContract");
            MainServer.Current.UserEnter(this);
            //OperationContext.Current.InstanceContext.Closing += (e, a) => MainServer.Current.UserLeave(this);
            //callback = OperationContext.Current.GetCallbackChannel<IChatDuplexCallback>();
            callback = Clients.Client(Context.ConnectionId);

        }

        /// <summary>
        /// Message for user
        /// </summary>
        public void SendReply(RoomDto room, MessageDto message)
        {
            if(authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Отправка сообщения пользователю");
                        callback.PrintMessage(room, message);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        /// <summary>
        /// Message from user
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(MessageDto message)
        {
            if (authorized)
            {
                Log.Trace("Отправка сообщения на сервер");
                MainServer.Current.SendMessage(this, message);
            }
            else
                Log.Trace("Отправка сообщения не удалась, пользователь не авторизован");
        }

        /// <summary>
        /// Change user image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public UserDto SetImage(ImageDto image)
        {
            if (authorized)
            {
                Log.Trace("Отправка изображения на сервер");
                Task.Factory.StartNew(() => MainServer.Current.SetAvatar(this, image));
                return User.ToUserDto(User);
            }
            else
            {
                Log.Trace("Установка изображения не удалась, пользователь не авторизован");
                return null;
            }
        }

        public void ChangeUserImage(RoomDto room, UserDto user, ImageDto image)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Изменить изображение");
                        callback.ChangeUserImage(user, image);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public UserDto UpdateProfile(LoginDto login)
        {
            var request = MainServer.Current.UpdateProfile(this, login);
            return request;
        }
        /// <summary>
        /// Authorization
        /// </summary>
        public UserDto Login(LoginDto login)
        {
            var request = MainServer.Current.Login(this, login);
            if (request != null)
            {
                GetStarted();
                Log.Info("Авторизация успешна, {0}", login.Name);
            }
            else
            {
                Log.Info("Авторизация не удалась, {0}", login.Name);
            }
            return request;
        }

        /// <summary>
        /// Registration
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public UserDto SignUp(LoginDto login)
        {
            var request = MainServer.Current.Registration(this, login);
            if (request != null)
            {
                GetStarted();
                Log.Info("Регистрация успешна, {0}", login.Name);
            }
            else
            {
                Log.Info("Регистрация не удалась, {0}", login.Name);
            }
            return request;
        }

        public bool CheckConnection()
        {
            Log.Debug("Соединение успешно");
            return true;
        }

        /// <summary>
        /// Get online users
        /// </summary>
        /// <returns></returns>
        public List<UserDto> GetOnlineUsers(RoomDto room)
        {
            Log.Trace("Получение пользователей");
            var task = Task.Factory.StartNew(() => MainServer.Current.GetOnlineUsers(room));
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Video stream
        /// </summary>
        /// <param name="video"></param>
        public void SendVideo(RoomDto room, List<byte[]> video)
        {
            if(authorized)
                try
                {
                    MainServer.Current.SendVideo(room, this, video);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                
        }

        public void AddRoomForVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.AddRoomForVideo(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }

        public void RemoveRoomFromVideo(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoomFromVideo(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }

        }
        /// <summary>
        /// Video for user
        /// </summary>
        /// <param name="video"></param>
        /// <param name="user"></param>
        public void SendVideoForUser(RoomDto room, List<byte[]> video, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.ShowVideo(room, video, user);
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void AddUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RemoveUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        Task.Factory.StartNew(() => callback.RemoveUser(room, user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void AddOnlineUser(RoomDto room, UserDto user)
        {
            if(authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Добавление {0}", user.Name);
                        Task.Factory.StartNew(() => { callback.AddOnlineUser(room, user); });
                    }
                    catch (System.Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }
        public void RemoveOnlineUser(RoomDto room, UserDto user)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", user.Name);
                        Task.Factory.StartNew(() => callback.RemoveOnlineUser(room, user));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RoomRemoved(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", room.Name);
                        Task.Factory.StartNew(() => callback.RoomRemoved(room));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void RoomUpdated(RoomDto room)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Log.Trace("Удаление {0}", room.Name);
                        Task.Factory.StartNew(() => callback.RoomUpdated(room));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public void UpdateProfileOnClientSide(UserDto user, string name)
        {
            if (authorized)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.UpdateProfileOnClientSide(user, name);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<MessageDto> LoadMessages(RoomDto room, int number, System.DateTime? date)
        {
            Log.Trace("Загрузка сообщений");
            var task = Task.Factory.StartNew(()=>MainServer.Current.LoadMessages(room, this, number, date));
            task.Wait();
            return task.Result;
        }

        public ImageDto GetAvatar(string name)
        {
            Log.Trace("Загрузка аватарки {0}", name);
            return MainServer.Current.GetAvatar(name);
        }

        public List<UserDto> GetUsers(RoomDto room)
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetUsers(room);
        }

        public List<UserDto> GetAllUsers()
        {
            Log.Trace("Получение пользователей");
            return MainServer.Current.GetAllUsers();
        }

        public bool IsOnline(RoomDto room, string name)
        {
            Log.Trace("{0} онлайн?", name);
            return MainServer.Current.IsOnline(room, name);
        }

        public int GetMaxFileLength()
        {
            var task = Task.Factory.StartNew(() => 1024);
            task.Wait();
            return task.Result;
        }

        private byte[] userFile;
        public byte[] GetUserFile() => userFile;
        public void ClearUserFile() => Array.Clear(userFile, 0, userFile.Length);

        public void PrepareFile(long length) => userFile = new byte[length];
        public void SendPartOfFile(FilePartDto file) => Task.Factory.StartNew(() => Array.Copy(file.Buffer, 0, userFile, file.StartIndex, file.Length));
        
        byte[] serverFile;
        public long GetFile(FileMessageDto file)
        {
            var task = Task.Factory.StartNew(() => MainServer.Current.GetFile(this, file));
            task.Wait();
            serverFile = task.Result;
            return serverFile.Length;
        }
        public byte[] DownloadFile(long recieved)
        {
            long length = Math.Min(GetMaxFileLength(), serverFile.Length - recieved);
            byte[] partOfFile = new byte[length];
            Array.Copy(serverFile, recieved, partOfFile, 0, length);
            return partOfFile;
        }
        public void DeleteFile()
        {
            serverFile = null;
        }

        public void Logout()
        {
            MainServer.Current.Logout(this);
        }

        public void SendVoice(List<byte[]> voice)
        {
            if (authorized)
                try
                {
                    MainServer.Current.SendVoice(this, voice);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void SendVoiceForUser(RoomDto room, List<byte[]> voice, UserDto user)
        {
            if (authorized && user.Name != User.Name)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        callback.PlaySound(room, voice, user);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                });
        }

        public List<RoomDto> GetRooms()
        {
            if (authorized)
                try
                {
                    return MainServer.Current.GetRooms(this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            return null;
        }

        public RoomDto CreateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    return MainServer.Current.CreateRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            return null;
        }

        public void RemoveRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.RemoveRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void UpdateRoom(RoomDto room)
        {
            if (authorized)
                try
                {
                    MainServer.Current.UpdateRoom(room, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public void InviteUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    MainServer.Current.InviteUser(room, user, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }
        public void KickUser(RoomDto room, UserDto user)
        {
            if (authorized)
                try
                {
                    MainServer.Current.KickUser(room, user, this);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }
    }
}