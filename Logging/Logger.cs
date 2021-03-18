using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.Logging
{
    public static class Logger
    {
        static Logger()
        {
            IsLoggingEnabled = true;
        }
        public enum LogSeverity
        {
            Information = 0,
            Warning = 1,
            Error = 2,
            Critical = 3,
        }

        /// <summary>
        /// Enables logging.  Off by default
        /// </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Gets and sets the log output directory
        /// </summary>
        public static System.IO.DirectoryInfo LogDirectory
        {
            get
            {

                return _logDirectory ?? new DirectoryInfo(DefaultLogPath);
            }
            set { _logDirectory = value; }
        }

        private static System.IO.DirectoryInfo _logDirectory;

        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        public static void Log(Logger.LogSeverity severity, string message)
        {
            if (IsLoggingEnabled && LogDirectory != null)
            {
                Instance.Log(severity, message);
            }
        }

        /// <summary>
        /// Logs a message with an exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        public static void Log(Exception ex, string message)
        {
            if (IsLoggingEnabled && LogDirectory != null)
            {
                Instance.Log(ex, message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(Exception ex, string format, params object[] args)
        {
            if (IsLoggingEnabled && LogDirectory != null)
            {
                Instance.Log(ex, String.Format(format, args));
            }
        }

        /// <summary>
        /// Logs a formatted message
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(Logger.LogSeverity severity, string format, params object[] args)
        {
            if (IsLoggingEnabled && LogDirectory != null)
            {
                Instance.Log(severity, String.Format(format, args));
            }
        }
        public static void Log(string text)
        {
            if (IsLoggingEnabled && LogDirectory != null)
            {
                Instance.LogText(text);
            }
        }

        private static ILogger Instance
        {
            get { return _instance ?? (_instance = new LogFileTracer(Logger.LogDirectory.FullName)); }
        }

        private static ILogger _instance;

        private static readonly string DefaultLogPath = Path.Combine(
                                                         Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                                         @"DocAI\Logs\");
    }
}
