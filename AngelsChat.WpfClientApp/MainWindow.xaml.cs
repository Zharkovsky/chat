using AngelsChat.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using AngelsChat.WpfClientApp.ViewModels;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace AngelsChat.WpfClientApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public ClientService _client = new ClientService();

        public MainWindow()
        {
            //Log config
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);
            string folderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AngelsChat");
            string filePath = System.IO.Path.Combine(folderPath, "UserLog.txt");
            if (!System.IO.Directory.Exists(folderPath))
                System.IO.Directory.CreateDirectory(folderPath);
            if (!File.Exists(filePath))
                File.Create(filePath);
            fileTarget.FileName = filePath;
            fileTarget.Layout = @"${longdate} ${level:upperCase=true} ${message} ${callsite:includeSourcePath=true} ${stacktrace:topFrames=10} ${exception:format=ToString} ${event-properties:property1}";
            var rule2 = new LoggingRule("*", LogLevel.Trace, fileTarget);
            config.LoggingRules.Add(rule2);
            LogManager.Configuration = config;
            //End Log config

            
            DataContext = new ChatViewModel(_client, ShowMessage);
            InitializeComponent();

            
        }

        public void ShowMessage(string text)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized && ShowTipMessages)
            {
                TrayIcon.BalloonTipText = text;
                TrayIcon.ShowBalloonTip(1000);
            }
        }

        
        public bool ShowTipMessages { get; set; }
        
        public void ShowHideTip(object sender, RoutedEventArgs e)
        {
            ShowTipMessages = !ShowTipMessages; 
        }

        public void Logout(object sender, RoutedEventArgs e)
        {
            _client.Logout();
            DataContext = new ChatViewModel(_client, ShowMessage);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e); // базовый функционал приложения в момент запуска
            createTrayIcon(); // создание нашей иконки
        }

        private System.Windows.Forms.NotifyIcon TrayIcon = null;
        private ContextMenu TrayMenu = null;

        private bool createTrayIcon()
        {
            bool result = false;
            if (TrayIcon == null)
            { // только если мы не создали иконку ранее
                TrayIcon = new System.Windows.Forms.NotifyIcon(); // создаем новую
                TrayIcon.Icon = new System.Drawing.Icon("../../favicon.ico"); // изображение для трея
                                                                           // обратите внимание, за ресурсом с картинкой мы лезем в свойства проекта, а не окна,
                                                                           // поэтому нужно указать полный namespace
                TrayIcon.Text = "Angels Chat"; // текст подсказки, всплывающей над иконкой
                TrayMenu = (ContextMenu)Resources["TrayMenu"]; // а здесь уже ресурсы окна и тот самый x:Key

                // сразу же опишем поведение при щелчке мыши, о котором мы говорили ранее
                // это будет просто анонимная функция, незачем выносить ее в класс окна
                TrayIcon.Click += delegate (object sender, EventArgs e) {
                    if ((e as System.Windows.Forms.MouseEventArgs).Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        // по левой кнопке показываем или прячем окно
                        ShowHideMainWindow(sender, null);

                    }
                    else
                    {
                        // по правой кнопке (и всем остальным) показываем меню
                        TrayMenu.IsOpen = true;
                        Activate(); // нужно отдать окну фокус, см. ниже
                    }
                };
                result = true;
            }
            else
            { // все переменные были созданы ранее
                result = true;
            }
            TrayIcon.Visible = true; // делаем иконку видимой в трее
            return result;
        }

        private void ShowHideMainWindow(object sender, RoutedEventArgs e)
        {
            TrayMenu.IsOpen = false; // спрячем менюшку, если она вдруг видима
            if (IsVisible)
            {// если окно видно на экране
             // прячем его
                Hide();
                // меняем надпись на пункте меню
                (TrayMenu.Items[0] as MenuItem).Header = "Открыть";
            }
            else
            { // а если не видно
              // показываем
                Show();
                // меняем надпись на пункте меню
                (TrayMenu.Items[0] as MenuItem).Header = "Свернуть";
                WindowState = CurrentWindowState;
                Activate(); // обязательно нужно отдать фокус окну,
                            // иначе пользователь сильно удивится, когда увидит окно
                            // но не сможет в него ничего ввести с клавиатуры
            }
        }

        private WindowState fCurrentWindowState = WindowState.Normal;
        public WindowState CurrentWindowState
        {
            get { return fCurrentWindowState; }
            set { fCurrentWindowState = value; }
        }

        // переопределяем встроенную реакцию на изменение состояния сознания окна
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e); // системная обработка
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                // если окно минимизировали, просто спрячем
                Hide();
                // и поменяем надпись на менюшке
                (TrayMenu.Items[0] as MenuItem).Header = "Открыть";
            }
            else
            {
                // в противном случае запомним текущее состояние
                CurrentWindowState = WindowState;
            }
        }

        private bool fCanClose = false;
        public bool CanClose
        { // флаг, позволяющий или запрещающий выход из приложения
            get { return fCanClose; }
            set { fCanClose = value; }
        }

        // переопределяем обработчик запроса выхода из приложения
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e); // встроенная обработка
            if (!CanClose)
            {    // если нельзя закрывать
                e.Cancel = true;// выставляем флаг отмены закрытия
                                // запоминаем текущее состояние окна
                CurrentWindowState = this.WindowState;
                // меняем надпись в менюшке
                (TrayMenu.Items[0] as MenuItem).Header = "Открыть";
                // прячем окно
                Hide();
            }
            else
            { // все-таки закрываемся
              // убираем иконку из трея
                TrayIcon.Visible = false;
            }
        }

        private void MenuExitClick(object sender, RoutedEventArgs e)
        {
            CanClose = true;
            _client.Logout();
            Close();
        }
    }
}
