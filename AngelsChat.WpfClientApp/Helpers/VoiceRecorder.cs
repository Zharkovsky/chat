using System;
using System.Linq;
using System.IO;
using AngelsChat.Shared.Data;

namespace AngelsChat.WpfClientApp.Helpers
{
    public class VoiceRecorder
    {
        public delegate void Sender(byte[] voice);
        public Sender _send;

        private WinSound.Recorder m_Recorder = new WinSound.Recorder();
        private Configuration Config = new Configuration();
        private String ConfigFileName = Path.Combine(Path.GetDirectoryName(Environment.CurrentDirectory), "config.xml");
        private bool m_IsFormMain = true;
        private System.Windows.Forms.Timer m_TimerProgressBarFile = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer m_TimerProgressBarJitterBuffer = new System.Windows.Forms.Timer();
        private int m_CurrentRTPBufferPos = 0;
        private Byte[] m_FilePayloadBuffer;
        private int m_RTPPartsLength = 0;
        Byte[] m_PartByte;
        private bool m_IsTimerStreamRunning = false;
        private uint m_Milliseconds = 20;
        private WinSound.EventTimer m_TimerStream = new WinSound.EventTimer();
        private bool m_Loop = false;
        private WinSound.JitterBuffer m_JitterBuffer;
        private uint m_RecorderFactor = 4;
        private uint m_JitterBufferCount = 20;
        private long m_SequenceNumber = 4596;
        private long m_TimeStamp = 0;
        private int m_Version = 2;
        private bool m_Padding = false;
        private bool m_Extension = false;
        private int m_CSRCCount = 0;
        private bool m_Marker = false;
        private int m_PayloadType = 0;
        private uint m_SourceId = 0;
        WinSound.WaveFileHeader m_FileHeader = new WinSound.WaveFileHeader();


        public void Init(Sender send)
        {
            _send = send;
            try
            {
                InitRecorder();
                InitTimerStream();
                InitJitterBuffer();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Init()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// InitRecorder
        /// </summary>
        private void InitRecorder()
        {
            m_Recorder.DataRecorded += new WinSound.Recorder.DelegateDataRecorded(OnDataReceivedFromSoundcard);
            m_Recorder.RecordingStopped += new WinSound.Recorder.DelegateStopped(OnRecordingStopped);
        }

        /// <summary>
        /// OnDataReceivedFromSoundcard
        /// </summary>
        /// <param name="linearData"></param>
        private void OnDataReceivedFromSoundcard(Byte[] data)
        {
            try
            {
                lock (this)
                {
                    if (_send != null)
                    {
                        //Wenn JitterBuffer
                        if (Config.UseJitterBuffer)
                        {
                            //Sounddaten in kleinere Einzelteile zerlegen
                            int bytesPerInterval = WinSound.Utils.GetBytesPerInterval((uint)Config.SamplesPerSecond, Config.BitsPerSample, Config.Channels);
                            int count = data.Length / bytesPerInterval;
                            int currentPos = 0;
                            for (int i = 0; i < count; i++)
                            {
                                //Teilstück in RTP Packet umwandeln
                                Byte[] partBytes = new Byte[bytesPerInterval];
                                Array.Copy(data, currentPos, partBytes, 0, bytesPerInterval);
                                currentPos += bytesPerInterval;
                                WinSound.RTPPacket rtp = ToRTPPacket(partBytes, Config.BitsPerSample, Config.Channels);
                                //In Buffer legen
                                m_JitterBuffer.AddData(rtp);
                            }
                        }
                        else
                        {
                            //Alles in RTP Packet umwandeln
                            Byte[] rtp = ToRTPData(data, Config.BitsPerSample, Config.Channels);
                            //Absenden
                            _send(rtp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// OnRecordingStopped
        /// </summary>
        private void OnRecordingStopped()
        {
            try
            {
                //Wenn JitterBuffer
                if (Config.UseJitterBuffer)
                {
                    m_TimerProgressBarJitterBuffer.Stop();
                    m_JitterBuffer.Stop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// ToRTPPacket
        /// </summary>
        /// <param name="linearData"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        private WinSound.RTPPacket ToRTPPacket(Byte[] linearData, int bitsPerSample, int channels)
        {
            //Daten Nach MuLaw umwandeln
            Byte[] mulaws = WinSound.Utils.LinearToMulaw(linearData, bitsPerSample, channels);

            //Neues RTP Packet erstellen
            WinSound.RTPPacket rtp = new WinSound.RTPPacket();

            //Werte übernehmen
            rtp.Data = mulaws;
            rtp.SourceId = m_SourceId;
            rtp.CSRCCount = m_CSRCCount;
            rtp.Extension = m_Extension;
            rtp.HeaderLength = WinSound.RTPPacket.MinHeaderLength;
            rtp.Marker = m_Marker;
            rtp.Padding = m_Padding;
            rtp.PayloadType = m_PayloadType;
            rtp.Version = m_Version;

            //RTP Header aktualisieren
            try
            {
                rtp.SequenceNumber = Convert.ToUInt16(m_SequenceNumber);
                m_SequenceNumber++;
            }
            catch (Exception)
            {
                m_SequenceNumber = 0;
            }
            try
            {
                rtp.Timestamp = Convert.ToUInt32(m_TimeStamp);
                m_TimeStamp += mulaws.Length;
            }
            catch (Exception)
            {
                m_TimeStamp = 0;
            }

            //Fertig
            return rtp;
        }

        /// <summary>
        /// ToRTPData
        /// </summary>
        /// <param name="linearData"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="channels"></param>
        /// <returns></returns>
        private Byte[] ToRTPData(Byte[] data, int bitsPerSample, int channels)
        {
            //Neues RTP Packet erstellen
            WinSound.RTPPacket rtp = ToRTPPacket(data, bitsPerSample, channels);
            //RTPHeader in Bytes erstellen
            Byte[] rtpBytes = rtp.ToBytes();
            //Fertig
            return rtpBytes;
        }

        /// <summary>
        /// InitTimerStream
        /// </summary>
        private void InitTimerStream()
        {
            m_TimerStream.TimerTick += new WinSound.EventTimer.DelegateTimerTick(OnTimerStream);
        }
        /// <summary>
        /// OnTimerStream
        /// </summary>
        /// <param name="lpParameter"></param>
        /// <param name="TimerOrWaitFired"></param>
        private void OnTimerStream()
        {
            try
            {
                //Wenn noch aktiv
                if (m_IsTimerStreamRunning)
                {
                    if ((m_CurrentRTPBufferPos + m_RTPPartsLength) <= m_FilePayloadBuffer.Length)
                    {
                        //Bytes senden
                        Array.Copy(m_FilePayloadBuffer, m_CurrentRTPBufferPos, m_PartByte, 0, m_RTPPartsLength);
                        m_CurrentRTPBufferPos += m_RTPPartsLength;
                        WinSound.RTPPacket rtp = ToRTPPacket(m_PartByte, m_FileHeader.BitsPerSample, m_FileHeader.Channels);
                        _send(rtp.ToBytes());
                    }
                    else
                    {
                        //Rest-Bytes senden
                        int rest = m_FilePayloadBuffer.Length - m_CurrentRTPBufferPos;
                        Byte[] restBytes = new Byte[m_PartByte.Length];
                        Array.Copy(m_FilePayloadBuffer, m_CurrentRTPBufferPos, restBytes, 0, rest);
                        WinSound.RTPPacket rtp = ToRTPPacket(restBytes, m_FileHeader.BitsPerSample, m_FileHeader.Channels);
                        _send(rtp.ToBytes());

                        if (m_Loop == false)
                        {
                            //QueueTimer beenden
                            StopTimerStream();
                        }
                        else
                        {
                            //Von vorne beginnen
                            m_CurrentRTPBufferPos = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                StopTimerStream();
            }
        }
        /// <summary>
        /// StopTimerStream
        /// </summary>
        private void StopTimerStream()
        {
            if (m_TimerStream.IsRunning)
            {
                //QueueTimer beenden
                m_TimerStream.Stop();

                //Variablen setzen
                m_IsTimerStreamRunning = m_TimerStream.IsRunning;
                //m_MulticastSender.Close();
                //m_MulticastSender = null;
                m_CurrentRTPBufferPos = 0;
                //OnFileStreamingEnd();
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
            m_JitterBuffer = new WinSound.JitterBuffer(null, m_JitterBufferCount, m_Milliseconds);
            m_JitterBuffer.DataAvailable += new WinSound.JitterBuffer.DelegateDataAvailable(OnDataAvailable);
        }

        /// <summary>
        /// OnDataAvailable
        /// </summary>
        /// <param name="packet"></param>
        private void OnDataAvailable(Object sender, WinSound.RTPPacket rtp)
        {
            try
            {
                if (_send != null)
                {
                    if (m_IsFormMain)
                    {
                        //RTP Packet in Bytes umwandeln
                        Byte[] rtpBytes = rtp.ToBytes();
                        //Absenden
                        _send(rtpBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public void StartRecord()
        {
            try
            {
                //Daten ermitteln
                FormToConfig();

                if (m_Recorder.Started == false)
                {
                    //Starten
                    StartRecording();
                    //ShowStarted_StreamSound();
                }
                else
                {
                    //Schliessen
                    m_Recorder.Stop();

                    //Wenn JitterBuffer
                    if (Config.UseJitterBuffer)
                    {
                        m_JitterBuffer.Stop();
                    }

                    //Warten bis Aufnahme beendet
                    while (m_Recorder.Started)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// StartRecording
        /// </summary>
        private void StartRecording()
        {
            try
            {
                //Buffer Grösse je nach JitterBuffer berechnen
                int bufferSize = 0;
                if (Config.UseJitterBuffer)
                {
                    bufferSize = WinSound.Utils.GetBytesPerInterval((uint)Config.SamplesPerSecond, Config.BitsPerSample, Config.Channels) * (int)m_RecorderFactor;
                }
                else
                {
                    bufferSize = WinSound.Utils.GetBytesPerInterval((uint)Config.SamplesPerSecond, Config.BitsPerSample, Config.Channels);
                }

                if (bufferSize > 0)
                {
                    if (m_Recorder.Start(Config.SoundDeviceName, Config.SamplesPerSecond, Config.BitsPerSample, Config.Channels, Config.BufferCount, bufferSize))
                    {
                        //ShowStarted_StreamSound();

                        //Wenn JitterBuffer
                        if (Config.UseJitterBuffer)
                        {
                            m_JitterBuffer.Start();
                            m_TimerProgressBarJitterBuffer.Start();
                        }
                    }
                    else
                    {
                        //ShowError();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("BufferSize must be greater than 0. BufferSize: {0}", bufferSize));
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// FormToConfig
        /// </summary>
        /// <returns></returns>
        private bool FormToConfig()
        {
            try
            {
                Config.SoundDeviceName = WinSound.WinSound.GetRecordingNames().FirstOrDefault();
                Config.SamplesPerSecond = 8000;
                Config.BitsPerSample = 16;
                Config.Channels = 2;
                Config.BufferCount = 8;
                Config.UseJitterBuffer = true;
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Fehler bei der Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }

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
        public String MulticasAddress = "";
        public String SoundDeviceName = "";
        public int MulticastPort = 0;
        public int SamplesPerSecond = 8000;
        public short BitsPerSample = 16;
        public short Channels = 2;
        public Int32 BufferCount = 8;
        public String FileName = "";
        public bool Loop = false;
        public bool UseJitterBuffer = true;
    }
}
