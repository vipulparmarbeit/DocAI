using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.Logging
{
    public class LogFileTracer : ILogger
    {
        // Name of the Log File, initialize to null to make checks faster when writing log entries
        private static string LogFilePath = null;

        private static string PathToLogDirectory  = null;

        /// <summary>
        /// Prefix for the log file name
        /// </summary>
        private string LogFileNamePrefix
        {
            get
            {
                var currentAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                //return "Plugins.AppTagProvider";
                return "Log." + currentAssembly.GetName().Name;
            }
        }

        // Writing semaphore for opening the log file and adding a new message to it
        static System.Threading.Semaphore _writeSemaphore = new System.Threading.Semaphore(1, 1);

        public LogFileTracer(string pathToLogDirectory)
        {
            if (!Directory.Exists(pathToLogDirectory))
            {
                Directory.CreateDirectory(pathToLogDirectory);
            }
            LogFilePath = Path.Combine(pathToLogDirectory,
                                       string.Format(@"{0}.{1}.log",
                                                     LogFileNamePrefix,
                                                     DateTime.Now.ToString(@"yy_MM_dd-hh-mm_ss")));

            PathToLogDirectory = pathToLogDirectory;
        }


        public void Log(Logger.LogSeverity severity, string message)
        {
            var text = String.Format("{0}\t\t{1}\t\t{2}", severity.ToString(), DateTime.Now.ToString(@"yyyy-MM-dd hh:mm:ss"), message);
            this.Log(text);
        }
        public void LogText(string message)
        {
            this.Log(message);
        }


        private void Log(string text)
        {
            if (text != null)
            {
                try
                {
                    _writeSemaphore.WaitOne();
                    StreamWriter txtWriter = null;
                   
                    if (!File.Exists(LogFilePath))
                    {
                        txtWriter = File.CreateText(LogFilePath);
                    }
                    else
                    {
                        txtWriter = File.AppendText(LogFilePath);
                        FileInfo into = new FileInfo(LogFilePath);
                        long aa = into.Length;
                        if (aa >= (1024 * 1024))
                        {
                            LogFilePath = Path.Combine(PathToLogDirectory,
                                      string.Format(@"{0}.{1}.log",
                                                    LogFileNamePrefix,
                                                    DateTime.Now.ToString(@"yy_MM_dd-hh-mm_ss")));
                           // File.Delete(LogFilePath);
                            txtWriter = File.CreateText(LogFilePath);
                        }
                    }

                    txtWriter.WriteLine(text);
                    txtWriter.Close();
                }

                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(text + Environment.NewLine + ex.Message);
                }

                finally
                {
                    _writeSemaphore.Release();
                }
            }
        }
        public void Log(Exception ex, string message)
        {
            var text = String.Format("Exception Occured at TimeStamp={0}\t Exceptiontype = {1}",
                                      DateTime.Now.ToString(@"yyyy-MM-dd hh:mm:ss"),
                                      ex.GetType()
                                      );
            this.Log("------------------------------------------------------------------------------------------------------------------------------------------------------------");
            this.Log(text);
            this.Log(message);
            this.Log("Message");
            this.Log("--------------------");
            this.Log(ex.Message);
            this.Log("StackTrace");
            this.Log("--------------------");
            this.Log(ex.StackTrace);
            this.Log("------------------------------------------------------------------------------------------------------------------------------------------------------------");
        }
    }
}
