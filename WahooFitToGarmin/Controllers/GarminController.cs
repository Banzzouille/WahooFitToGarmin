using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WahooFitToGarmin.Services;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GarminController : Controller
    {
        private const int Timeout = 2000;
        private readonly IGarminConnectSettingsService _garminConnectSettingsService;
        private readonly ILogger<GarminController> _logger;

        public GarminController( IGarminConnectSettingsService garminConnectSettingsService, ILogger<GarminController> logger)
        {
            _garminConnectSettingsService = garminConnectSettingsService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult SendFile()
        {
            _logger.LogInformation($"{DateTime.Now} ==> Enter SendFile GET method");
            _logger.LogInformation($"========================================================");
            

            Task.Delay(Timeout).Wait();
            System.Console.WriteLine("-------------------------------------------------------------------------------");

            var allFitFiles = Directory.GetFiles("Activities", "*.fit");

            var fitFile = allFitFiles.FirstOrDefault();
            if (fitFile == null) return Ok();

           

            return Ok();
        }
    }
}
