using System.Resources;

namespace System.Windows.Forms
{
    internal static class ResourceHelper
    {
        private static ResourceManager _resourceManager;

        private static ResourceManager ResourceManager => _resourceManager ??
                                                          (_resourceManager =
                                                              new ResourceManager("System.Windows.Forms.Strings",
                                                                  typeof (ResourceHelper).Assembly));

        public static string GetString(string name)
        {
            return ResourceManager.GetString(name);
        }
    }
}
