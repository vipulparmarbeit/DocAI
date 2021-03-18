using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.Logging
{
    /// <summary>
    /// A logger capable of writing logs
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="message"></param>
        void Log(Logger.LogSeverity severity, string message);
        /// <summary>
        /// Logs a message and exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        void Log(Exception ex, string message);

        void LogText(string message);

    }

}
