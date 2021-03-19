using System;

namespace WahooFitToGarmin.Services
{
    public class GarminConnectSettingsService : IGarminConnectSettingsService
    {
        public string GetGarminConnectUserName()
        {
            return Environment.GetEnvironmentVariable("GarminConnectUserName");
        }
        public string GetGarminConnectPassword()
        {
            return Environment.GetEnvironmentVariable("GarminConnectPassword");
        }
    }
}
