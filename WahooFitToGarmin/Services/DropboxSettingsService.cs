using System;

namespace WahooFitToGarmin.Services
{
    public class DropboxSettingsService : IDropboxSettingsService
    {
        public string GetDropboxAppName()
        {
            return Environment.GetEnvironmentVariable("DropboxAppName");
        }

        public string GetDropboxAppToken()
        {
            return Environment.GetEnvironmentVariable("DropboxAppToken");
        }
    }

}
