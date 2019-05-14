using System;
using NLog;
using NLog.Config;
using AngelsChat.Server.Settings;
using System.Linq;
using AngelsChat.Server.Communication;

namespace AngelsChat.ConsoleServer
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            Settings Settings = Settings.Read();
            //Log config
            var config = new LoggingConfiguration();

            LogLevel logLevel = LogLevel.FromString(Settings.Log.Lvl);
            config.AddTarget(Settings.Log.Rule.GetRule(logLevel).Targets.First());
            config.LoggingRules.Add(Settings.Log.Rule.GetRule(logLevel));
            LogManager.Configuration = config;

            //WcfServerHost host = new WcfServerHost();
            SignalRServerHost host = new SignalRServerHost();

            host.Start(Settings);

            Console.WriteLine("Сервис запущен; Нажмите любую кнопку чтобы завершить...");
            Console.ReadLine();

            host.Stop();
            Console.WriteLine("Сервис остановлен");
        }
    }
}
