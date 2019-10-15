using log4net;
using System.Diagnostics;

namespace Framework
{
    public static class Logger
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Environment.MachineName);

        public static void LogInfo()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(0);

            var currentMethodName = sf.GetMethod();

            Log.Info(currentMethodName);
        }
    }
}
