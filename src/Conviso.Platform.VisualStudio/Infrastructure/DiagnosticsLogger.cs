using Microsoft.VisualStudio.Shell;

namespace Conviso.Platform.VisualStudio.Infrastructure
{
    internal static class DiagnosticsLogger
    {
        private const string Source = "Conviso Platform";

        public static void LogInfo(string message)
        {
            ActivityLog.TryLogInformation(Source, message);
        }

        public static void LogWarning(string message)
        {
            ActivityLog.TryLogWarning(Source, message);
        }

        public static void LogError(string message)
        {
            ActivityLog.TryLogError(Source, message);
        }
    }
}
