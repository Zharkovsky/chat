using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AngelsChat.WpfClientApp.Helpers
{
    public class Settings
    {
        public string Ip { get; set; }
        public string Port { get; set; }

        public Settings()
        {
            Ip = "localhost";
            Port = "9080";
        }
        public Settings(string _Ip, string _Port)
        {
            Ip = _Ip;
            Port = _Port;
        }

        public static Settings Read()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat");
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat\\Settings.xml");
            Settings settings = null;
            if (!Directory.Exists(folderPath) || !File.Exists(filePath))
            {
                settings = new Settings();
                settings.Write();
            }
            else
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    settings = (Settings)serializer.Deserialize(stream);
                }
            }
            return settings;

        }

        public void Write()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat");
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat\\Settings.xml");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                serializer.Serialize(stream, this);
            }
        }
    }
}
