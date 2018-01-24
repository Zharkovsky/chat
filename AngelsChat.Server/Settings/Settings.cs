using System;
using System.IO;
using System.Xml.Serialization;

namespace AngelsChat.Server.Settings
{
    [Serializable]
    public class Settings
    {
        public ConnectionSettings Connection;
        public LogSettings Log;
        public EfSettings Ef;

        public Settings()
        {
            Connection = new ConnectionSettings { Ip = "localhost", Port = "9080" };
            Log = new LogSettings { Lvl = "Trace", Rule = new FileRuleLogSettings { FileSource = SettingsPath.FolderPath } };
            Ef = new EfSettings { Source = "(localdb)\\MSSQLLocalDB", Name = "chatdatabase2" };
        }
        
        public static Settings Read()
        {
            Settings settings = null;
            if (!Directory.Exists(SettingsPath.FolderPath) || !File.Exists(SettingsPath.FilePath))
            {
                settings = new Settings();
                settings.Write();
            }
            else
            {
                using (FileStream stream = new FileStream(SettingsPath.FilePath, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    settings = (Settings)serializer.Deserialize(stream);
                }
            }
            return settings;

        }

        public void Write()
        {
            if (!Directory.Exists(SettingsPath.FolderPath))
                Directory.CreateDirectory(SettingsPath.FolderPath);
            
            using (FileStream stream = new FileStream(SettingsPath.FilePath, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(stream, this);
            }
        }
    }
}
