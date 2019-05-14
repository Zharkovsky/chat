using AngelsChat.Server.ContractImplementations;
using AngelsChat.Shared.Operations;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatSettings = AngelsChat.Server.Settings.Settings;

namespace AngelsChat.Server.Communication
{
    public class SignalRServerHost : ServerHost, IDisposable
    {
        private IDisposable _webApp;
        private IDependencyResolver _resolver;

        public IDependencyResolver DependencyResolver =>
            _resolver ?? (_resolver = new DefaultDependencyResolver());

        public void DependencyRegister<T>(T instance)
        {
            DependencyResolver.Register(typeof(T), () => instance);
        }

        public void DependencyRegister<T>(Func<T> factory)
        {
            DependencyResolver.Register(typeof(T), () => factory());
        }

        public override void Start(ChatSettings serverSettings)
        {
            string url = "http://*:9080/";
            Settings = serverSettings;
            InitializeHost();
            _webApp = WebApp.Start(url, Configuration);
        }

        public override void Stop()
        {
            _webApp?.Dispose();
            _webApp = null;
        }

        public void Dispose()
        {
            Stop();
            _resolver?.Dispose();
            _resolver = null;
        }

        private void InitializeHost()
        {
            GlobalHost.DependencyResolver = DependencyResolver;
            IConfigurationManager connectionManager = new DefaultConfigurationManager
            {
                DefaultMessageBufferSize = 1
            };
            DependencyResolver.Register(typeof(IConfigurationManager), () => connectionManager);
            DependencyResolver.Register(typeof(UserContract), ActivationExtensions.ResolveSystemHub);
        }

        private void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }

    internal static class ActivationExtensions
    {
        public static UserContract ResolveSystemHub()
        {
            return new UserContract();
        }
    }

    //public class SystemHub : Hub
    //{
    //    private readonly IServerContract _system;

    //    private SystemHubClient CurrentClient
    //    {
    //        get => new SystemHubClient(Context.ConnectionId, this);
    //    }

    //    public SystemHub(IServerContract system)
    //    {
    //        _system = system;
    //    }

    //    public override Task OnDisconnected(bool stopCalled)
    //    {
    //        _system.Unsubscribe(CurrentClient);
    //        return base.OnDisconnected(stopCalled);
    //    }

    //    public bool IsSubscribed()
    //    {
    //        return _system.IsSubscribed(CurrentClient);
    //    }

    //    public void Subscribe()
    //    {
    //        _system.(CurrentClient);
    //    }

    //    public void Unsubscribe()
    //    {
    //        _system.Unsubscribe(CurrentClient);
    //    }
    //}

    //public class SystemHubClient : IChatDuplexCallback
    //{
    //    private readonly string _connectionId;
    //    private readonly SystemHub _systemHub;

    //    public SystemHubClient(string connectionId, SystemHub systemHub)
    //    {
    //        _connectionId = connectionId;
    //        _systemHub = systemHub;
    //    }

    //    public void Disconnect()
    //    {
    //        _systemHub.Clients.Client(_connectionId).Disconect();
    //    }

    //    public void Warning(NodeWarning warning)
    //    {
    //        _systemHub.Clients.Client(_connectionId).Warning(warning);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return _connectionId.GetHashCode();
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        return Equals(obj as SystemHubClient);
    //    }

    //    public bool Equals(SystemHubClient other)
    //    {
    //        return _connectionId == other?._connectionId;
    //    }
    //}

}
