using log4net;
using System.Diagnostics;

namespace Framework
{
    public static class Logger
    {
        private static readonly ILog _Log = LogManager.GetLogger(System.Environment.MachineName);

        public enum LogAction
        {
            Insertar = 1,
            Modificar = 2,
            Eliminar = 3,

        }
        public enum LogType
        {
            Info = 1,
            Exception = 2

        }

        public static void Log(LogAction action, string className, int idEntity, LogType type, string exMessage)
        {
            LogicalThreadContext.Properties["IdUser"] = Helpers.Session.User.IdUsuario;
            var logMessage = "{0} {1} - Id: {2}";

            switch (action)
            {
                case LogAction.Insertar:
                    logMessage = string.Format(logMessage, "Insertar", className, idEntity.ToString());
                    break;
                case LogAction.Modificar:
                    logMessage = string.Format(logMessage, "Modificar", className, idEntity.ToString());
                    break;
                case LogAction.Eliminar:
                    logMessage = string.Format(logMessage, "Eliminar", className, idEntity.ToString());
                    break;
            }

            switch (type)
            {
                case LogType.Info:
                    LogInfo(logMessage);
                    break;

                case LogType.Exception:
                    logMessage += " - Error: " + exMessage;
                    LogException(logMessage);
                    break;
            }
        }

        public static void LogInfo(string message)
        {
            _Log.Info(message);
        }

        private static void LogException(string message)
        {
            _Log.Error(message);
        }
    }
}
