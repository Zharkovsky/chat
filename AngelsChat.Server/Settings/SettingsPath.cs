using System;
using System.IO;

namespace AngelsChat.Server.Settings
{
    public static class SettingsPath
    {
        public static string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AngelsChat");
        public static string FilePath = Path.Combine(FolderPath, "Settings.xml");
    }
}