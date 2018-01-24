using System;
using AngelsChat.Server;
using AngelsChat.Server.Data;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using AngelsChat.Server.Settings;
using System.Linq;
using System.ServiceModel;
using AngelsChat.Shared.Operations;

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

            ServerHost host = new ServerHost();
            host.Start(Settings);
            

            Console.WriteLine("Сервис запущен; Нажмите любую кнопку чтобы завершить...");
            Console.ReadLine();

            host.Stop();
            Console.WriteLine("Сервис остановлен");
        }
    }
}
