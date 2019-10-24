using log4net;
using System.Diagnostics;

namespace Framework
{
    public static class Logger
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Environment.MachineName);

        public static void LogInfo(string message)
        {
            LogicalThreadContext.Properties["IdUser"] = Helpers.Session.User.IdUsuario;

            Log.Info(message);
        }

        public static void LogException(string message)
        {
            LogicalThreadContext.Properties["IdUser"] = Helpers.Session.User.IdUsuario;

            Log.Error(message);
        }
    }
}
