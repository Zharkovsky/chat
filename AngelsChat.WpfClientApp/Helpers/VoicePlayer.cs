using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;

namespace AngelsChat.WpfClientApp.Helpers
{
    public class VoicePlayer
    {
        WinSound.Player m_Player;
        List<String> m_Data = new List<string>();
        private Configuration Config = new Configuration();
        private String ConfigFileName = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), "config.xml");
        Graphics GraphicsPanelCurve;
        Pen PenCurve;
        Byte[] m_BytesToDraw;
        System.Windows.Forms.Timer m_TimerDrawCurve;
        System.Windows.Forms.Timer m_TimerDrawProgressBar;
        System.Windows.Forms.Timer m_TimerDrawMeasurements;
        bool IsDrawCurve = false;
        private WinSound.JitterBuffer m_JitterBuffer = new WinSound.JitterBuffer(null, 20, 0);
        private uint m_JitterBufferLength = 20;
        private WinSound.Stopwatch m_Stopwatch = new WinSound.Stopwatch();
        private double m_MeasurementTimeOne = 0;
        private double m_MeasurementTimeTwo = 0;
        private Queue<double> m_QueueTimeDiffs = new Queue<double>();
        bool m_TimeMeasurementToggler = false;

        /// <summary>
        /// Config
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Config
            /// </summary>
            public Configuration()
            {

            }

            //Attribute
            public String SoundDeviceName = WinSound.WinSound.GetPlaybackNames().FirstOrDefault();
            public int MulticastPort = 0;
            public int SamplesPerSecond = 8000;
            public short BitsPerSample = 16;
            public short Channels = 1;
            public Int32 PacketSize = 4096;
            public Int32 BufferCount = 8;
            public uint JitterBuffer = 20;
        }
        /// <summary>
        /// Start
        /// </summary>
        public void Init()
        {
            try
            {
                //WinSoundServer
                m_Player = new WinSound.Player();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Fehler beim Initialisieren", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Start()
        {
            try
            {
                    //Wenn geöffnet
                    if (m_Player.Opened)
                    {
                        //Wenn JitterBuffer
                        if (UseJitterBuffer)
                        {
                            m_JitterBuffer.Stop();
                        }
                        
                    }
                    else
                    {
                            //Zeitmessungen zurücksetzen
                            ResetTimeMeasurements();

                            //WinSound Player öffnen
                            m_Player.Open(Config.SoundDeviceName, Config.SamplesPerSecond, Config.BitsPerSample, Config.Channels, Config.BufferCount);

                            //Wenn JitterBuffer
                            if (UseJitterBuffer)
                            {
                                InitJitterBuffer();
                                m_JitterBuffer.Start();
                            }
                    }
                    
                
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
		/// InitJitterBuffer
		/// </summary>
		private void InitJitterBuffer()
        {
            //Wenn vorhanden
            if (m_JitterBuffer != null)
            {
                m_JitterBuffer.DataAvailable -= new WinSound.JitterBuffer.DelegateDataAvailable(OnDataAvailable);
            }

            //Neu erstellen
            m_JitterBuffer = new WinSound.JitterBuffer(null, (uint)20, 20);
            m_JitterBuffer.DataAvailable += new WinSound.JitterBuffer.DelegateDataAvailable(OnDataAvailable);
            
        }
        /// <summary>
		/// OnDataAvailable
		/// </summary>
		/// <param name="packet"></param>
		private void OnDataAvailable(Object sender, WinSound.RTPPacket rtp)
        {
            //Nach Linear umwandeln
            Byte[] linearBytes = WinSound.Utils.MuLawToLinear(rtp.Data, Config.BitsPerSample, Config.Channels);
            //Abspielen
            m_Player.PlayData(linearBytes, false);
        }
        /// <summary>
		/// ResetTimeMeasurements
		/// </summary>
		private void ResetTimeMeasurements()
        {
            m_QueueTimeDiffs.Clear();
            m_MeasurementTimeOne = 0;
            m_MeasurementTimeTwo = 0;
        }
        /// <summary>
		/// Gibt an ob der Jitter Buffer verwendet werden soll
		/// </summary>
		/// <returns></returns>
		private bool UseJitterBuffer
        {
            get
            {
                if (m_JitterBuffer != null)
                {
                    return m_JitterBufferLength >= 2;
                }
                return false;
            }
        }

        /// <summary>
		/// OnDataReceived
		/// </summary>
		/// <param name="strMessage"></param>
        public void OnDataReceived(List<Byte[]> audio)
        {
            audio.ForEach(bytes =>
            {
                try
                {
                    //Wenn der Player gestartet wurde
                    if (m_Player.Opened)
                    {
                        //RTP Header auslesen
                        WinSound.RTPPacket rtp = new WinSound.RTPPacket(bytes);

                        //Wenn Anzeige
                        if (IsDrawCurve)
                        {
                            TimeMeasurement();
                            m_BytesToDraw = rtp.Data;
                        }

                        //Wenn Header korrekt
                        if (rtp.Data != null)
                        {
                            //Wenn JitterBuffer verwendet werden soll
                            if (UseJitterBuffer)
                            {
                                m_JitterBuffer.AddData(rtp);
                            }
                            else
                            {
                                //Nach Linear umwandeln
                                Byte[] linearBytes = WinSound.Utils.MuLawToLinear(rtp.Data, Config.BitsPerSample, Config.Channels);
                                //Abspielen
                                m_Player.PlayData(linearBytes, false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("FormMain.cs | OnDataReceived() | {0}", ex.Message));
                }
            });            
        }
        /// <summary>
		/// TimeMeasurement
		/// </summary>
		private void TimeMeasurement()
        {
            try
            {
                //Messen
                if (m_MeasurementTimeOne == 0)
                {
                    m_MeasurementTimeOne = m_Stopwatch.ElapsedMilliseconds;
                }
                else if (m_MeasurementTimeTwo == 0)
                {
                    m_MeasurementTimeTwo = m_Stopwatch.ElapsedMilliseconds;
                }

                //Wenn Messung komplett
                if (m_MeasurementTimeOne != 0 && m_MeasurementTimeTwo != 0)
                {
                    if (m_TimeMeasurementToggler)
                    {
                        m_QueueTimeDiffs.Enqueue(m_MeasurementTimeOne - m_MeasurementTimeTwo);
                        m_MeasurementTimeTwo = 0;
                    }
                    else
                    {
                        m_QueueTimeDiffs.Enqueue(m_MeasurementTimeTwo - m_MeasurementTimeOne);
                        m_MeasurementTimeOne = 0;
                    }
                    //Nächste Messung vorbereiten
                    m_TimeMeasurementToggler = !m_TimeMeasurementToggler;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("FormMain.cs | TimeMeasurement() | {0}", ex.Message));
            }
        }
    }


    
}