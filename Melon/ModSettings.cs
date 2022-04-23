using MelonLoader;

namespace CustomAlbums
{
    public static class ModSettings
    {
        private static string categoryName = "CustomAlbums"; 
        

        private static MelonPreferences_Entry<bool> debugLogging;
        
        public static bool DebugLoggingEnabled => debugLogging.Value;
        

        public static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory(categoryName, categoryName);
            debugLogging = category.CreateEntry("EnableDebugLogging", false, "Enable Debug Logging");
        }
    }
}