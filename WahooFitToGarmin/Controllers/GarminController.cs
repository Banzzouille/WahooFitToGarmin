using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WahooFitToGarmin.Services;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GarminController : Controller
    {
        private const string GarminUploaderFolderPath = "/usr/local/lib/python3.7/dist-packages/garmin_uploader/";
        private const string ActivityFolderPath = "Activities";
        private readonly IGarminConnectSettingsService _garminConnectSettingsService;
        private readonly IGarminUploader _garminUploader;
        private readonly ILogger<GarminController> _logger;

        public GarminController(IGarminConnectSettingsService garminConnectSettingsService,IGarminUploader garminUploader, ILogger<GarminController> logger)
        {
            _garminConnectSettingsService = garminConnectSettingsService;
            _garminUploader = garminUploader;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult SendFile()
        {
            _logger.LogInformation($"{DateTime.Now} ==> Enter SendFile GET method");
            _logger.LogInformation($"========================================================");
            
            if (!Directory.Exists(GarminUploaderFolderPath))
            {
                _logger.LogInformation($"{DateTime.Now} ==> Garmin_uploader folder not found");
                _logger.LogInformation($"========================================================");
                return Ok();
            }

            if (!Directory.Exists(ActivityFolderPath))
            {
                _logger.LogInformation($"{DateTime.Now} ==> Activity folder not found");
                _logger.LogInformation($"========================================================");
                return Ok();
            }

            var allFitFiles = Directory.GetFiles(ActivityFolderPath, "*.fit");

            var fitFile = allFitFiles.FirstOrDefault();
            if (fitFile == null) return Ok();
            _logger.LogInformation($"{DateTime.Now} ==> Working on this file : {fitFile}");

            _garminUploader.UploadAsync(_garminConnectSettingsService.GetGarminConnectUserName(),
                _garminConnectSettingsService.GetGarminConnectPassword(), fitFile, _logger).ConfigureAwait(false);

            _logger.LogInformation("-------------------------------------------------------------------------------");
            return Ok();
        }

    }
}
