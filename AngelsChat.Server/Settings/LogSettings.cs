using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Xml.Serialization;

namespace AngelsChat.Server.Settings
{
    public class LogSettings
    {
        public string Lvl { get; set; }

        public RuleLogSettings Rule { get; set; }

        public LogSettings() { }
    }

    [XmlInclude(typeof(FileRuleLogSettings))]
    [XmlInclude(typeof(DatabaseRuleLogSettings))]
    [XmlRoot("RuleLogSettings")]
    public abstract class RuleLogSettings
    {
        public RuleLogSettings() { }
        public abstract LoggingRule GetRule(LogLevel logLevel);

        //public abstract FileLogSettingsViewModel ToFileLogSettingsViewModel();
        //public abstract DatabaseRuleLogSettingsViewModel ToDatabaseRuleLogSettingsViewModel();

        //public abstract void FileLogSettingsFromViewModel(FileLogSettingsViewModel fileLogSettingsViewModel);
        //public abstract void DataBaseLogSettingsFromViewModel(DatabaseRuleLogSettingsViewModel databaseRuleLogSettingsViewModel);
    }

    public class FileRuleLogSettings : RuleLogSettings
    {
        public FileRuleLogSettings() { }
        public string FileSource { get; set; }

        public override LoggingRule GetRule(LogLevel logLevel)
        {
            var fileTarget = new FileTarget();
            string folderPath = FileSource;
            string filePath = System.IO.Path.Combine(folderPath, "ServerLog.txt");
            fileTarget.FileName = filePath;
            fileTarget.Layout = @"${longdate} ${level:upperCase=true} ${message} ${callsite:includeSourcePath=true} ${stacktrace:topFrames=10} ${exception:format=ToString} ${event-properties:property1}";
            fileTarget.Name = "file";
            var fileRule = new LoggingRule("*", logLevel, fileTarget);
            return fileRule;
        }

        //public override FileLogSettingsViewModel ToFileLogSettingsViewModel() => new FileLogSettingsViewModel { FileSource = this.FileSource };
        //public override DatabaseRuleLogSettingsViewModel ToDatabaseRuleLogSettingsViewModel() => null;

        //public override void FileLogSettingsFromViewModel(FileLogSettingsViewModel fileLogSettingsViewModel) => FileSource = fileLogSettingsViewModel.FileSource;
        //public override void DataBaseLogSettingsFromViewModel(DatabaseRuleLogSettingsViewModel databaseRuleLogSettingsViewModel) { }
    }
    
    public class DatabaseRuleLogSettings : RuleLogSettings
    {
        public string MsSQLSource { get; set; }
        public string MsSQLName { get; set; }

        public override LoggingRule GetRule(LogLevel logLevel)
        {
            var dbTarget = new DatabaseTarget();
            dbTarget.ConnectionString = $"data source={MsSQLSource};Initial Catalog={MsSQLName};Integrated Security=True;";
            dbTarget.CommandText = @"INSERT INTO [Log] (Date, Thread, Level, Logger, Message, Exception) VALUES(GETDATE(), @thread, @level, @logger, @message, @exception)";
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@thread", new NLog.Layouts.SimpleLayout("${threadid}")));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@level", new NLog.Layouts.SimpleLayout("${level}")));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@logger", new NLog.Layouts.SimpleLayout("${logger}")));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@message", new NLog.Layouts.SimpleLayout("${message}")));
            dbTarget.Parameters.Add(new DatabaseParameterInfo("@exception", new NLog.Layouts.SimpleLayout("${exception}")));
            dbTarget.Name = "db";
            var dbRule = new LoggingRule("*", logLevel, dbTarget);
            return dbRule;
        }

        //public override FileLogSettingsViewModel ToFileLogSettingsViewModel() => null;
        //public override DatabaseRuleLogSettingsViewModel ToDatabaseRuleLogSettingsViewModel() => new DatabaseRuleLogSettingsViewModel { MsSQLSource = MsSQLSource, MsSQLName = MsSQLName };

        //public override void FileLogSettingsFromViewModel(FileLogSettingsViewModel fileLogSettingsViewModel) { }
        //public override void DataBaseLogSettingsFromViewModel(DatabaseRuleLogSettingsViewModel databaseRuleLogSettingsViewModel)
        //{
        //    MsSQLSource = databaseRuleLogSettingsViewModel.MsSQLSource;
        //    MsSQLName = databaseRuleLogSettingsViewModel.MsSQLName;
        //}
    }
}
