using System;
using System.ServiceProcess;
using System.Threading;
using AngelsChat.Server;
using AngelsChat.Server.Data;
using NLog;
using AngelsChat.Server.Core;
using NLog.Targets;
using NLog.Config;
using System.IO;
using System.Linq;
using AngelsChat.Server.Settings;
using AngelsChat.Server.Communication;

namespace AngelsChat.WindowsService
{
    public partial class AngelsChatService : ServiceBase
    {
        public ServerHost host;
        private static Logger Log = LogManager.GetCurrentClassLogger();
        Settings Settings;
        public AngelsChatService()
        {
            //Thread.Sleep(10000);
            InitializeComponent();
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.AutoLog = true;

            Settings = Settings.Read();
            //Log config
            var config = new LoggingConfiguration();

            LogLevel logLevel = LogLevel.FromString(Settings.Log.Lvl);
            config.AddTarget(Settings.Log.Rule.GetRule(logLevel).Targets.First());
            config.LoggingRules.Add(Settings.Log.Rule.GetRule(logLevel));
            LogManager.Configuration = config;
        }

        protected override void OnStart(string[] args)
        {
            host = new WcfServerHost();
            host.Start(Settings);
        }

        protected override void OnStop()
        {
            host.Stop();
        }
    }
}
