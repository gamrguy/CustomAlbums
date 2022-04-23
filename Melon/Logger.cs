using MelonLoader;

namespace CustomAlbums
{
    public class Logger
    {
        private MelonLogger.Instance Log;

        public Logger(string sourceName) {
            Log = new MelonLogger.Instance(sourceName);
        }

        public void Debug(object data) {
            if(!ModSettings.DebugLoggingEnabled)
                return;

            Log.Msg(data);
        }

        public void Info(object data) {
            Log.Msg(data);
        }

        public void Warning(object data) {
            Log.Msg(System.ConsoleColor.Yellow, data);
        }

        public void Error(object data) {
            Log.Msg(System.ConsoleColor.Red, data);
        }
    }
}
