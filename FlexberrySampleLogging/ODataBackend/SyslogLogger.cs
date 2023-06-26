using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

namespace IIS.FlexberrySampleLogging
{
    public class SyslogLogger : ILogger
    {
        private const int syslogFacility = 16;

        private string categoryName;
        private string host;
        private int port;

        private readonly Func<string, LogLevel, bool> filter;

        public SyslogLogger(string categoryName,
                            string host,
                            int port,
                            Func<string, LogLevel, bool> filter)
        {
            this.categoryName = categoryName;
            this.host = host;
            this.port = port;

            this.filter = filter;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (filter == null || filter(categoryName, logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = $"{logLevel}: {message}";

            if (exception != null)
            {
                message += Environment.NewLine + Environment.NewLine + exception.ToString();
            }

            var syslogLevel = MapToSyslogLevel(logLevel);
            Send(syslogLevel, message);
        }

        internal void Send(SyslogLogLevel logLevel, string message)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0)
            {
                return;
            }

            /*
             * Пример SysLog сообщения в формате RFC 5424
             * <165>1 2003-08-24T05:14:15.000003-07:00 192.0.2.1 myproc 8710 - - TestMessage1
             *
             * <165> - приоритет, расичтывается как 8 * Facility(код источника) + Severity(важность).
             * 1 - Version
             * 2003-08-24T05:14:15.000003-07:00 - Дата-Время.Тип Timestamp
             * 192.0.2.1 - хост источника сообщения.
             * myproc - APP-NAME.
             * 8710 - PROCID.
             * STRUCTURED-DATA, что должно идти после PROCID в данном примере нет, поэтому -
             * Идентификатора сообщения (MSGID), что должен идти после STRUCTURED-DATA в данном примере нет, поэтому -
             * Само сообщение.
             */

            int priority = syslogFacility * 8 + (int)logLevel;
            int version = 1;
            string dateTime = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ");
            string hostName = Dns.GetHostName();
            string appName = "MyExampleApp";
            int procId = 8710;

            string logMessage = string.Format("<{0}>{1} {2} {3} {4} {5} - - {6}", priority, version, dateTime, hostName, appName, procId, message);
            logMessage += Environment.NewLine;

            var bytes = Encoding.UTF8.GetBytes(logMessage);

            using (TcpClient client = new TcpClient(host, port))
            {
                NetworkStream stream = client.GetStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
        }

        private SyslogLogLevel MapToSyslogLevel(LogLevel level)
        {
            if (level == LogLevel.Critical)
                return SyslogLogLevel.Critical;
            if (level == LogLevel.Debug)
                return SyslogLogLevel.Debug;
            if (level == LogLevel.Error)
                return SyslogLogLevel.Error;
            if (level == LogLevel.Information)
                return SyslogLogLevel.Info;
            if (level == LogLevel.None)
                return SyslogLogLevel.Info;
            if (level == LogLevel.Trace)
                return SyslogLogLevel.Info;
            if (level == LogLevel.Warning)
                return SyslogLogLevel.Warn;

            return SyslogLogLevel.Info;
        }
    }
}
