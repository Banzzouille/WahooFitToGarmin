using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WahooFitToGarmin.Services;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IDropboxSettingsService _dropboxSettingsService;

        public UploadController(ILogger<UploadController> logger, IDropboxSettingsService dropboxSettingsServiceService)
        {
            _logger = logger;
            _dropboxSettingsService = dropboxSettingsServiceService;
            _logger.Log(LogLevel.Information, $"DropboxAppName: {_dropboxSettingsService.GetDropboxAppName()}   DropboxAppToken:{_dropboxSettingsService.GetDropboxAppToken()}");
        }

        [HttpGet]
        public ActionResult<string> Verify(string challenge)
        {
            _logger.Log(LogLevel.Information, $"Enter Verify GET method");
            _logger.Log(LogLevel.Information,$"challenge received : {challenge}");
            _logger.Log(LogLevel.Information, $"========================================================");
            return challenge;
        }

        [HttpPost]
        public ActionResult<string> GetNotification([FromBody] string dataReceived)
        {
            _logger.Log(LogLevel.Information, $"Enter GetNotification POST method");
            // Receive a list of changed user IDs from Dropbox and process each.


            // Make sure this is a valid request from Dropbox
            StringValues headerValue;
            Request.Headers.TryGetValue("X-Dropbox-Signature", out headerValue);
            var headerValueResult = headerValue.FirstOrDefault();
            _logger.Log(LogLevel.Information, $"headerValueResult: {headerValueResult}");



            _logger.Log(LogLevel.Information, $"dataReceived: {dataReceived}");
            _logger.Log(LogLevel.Information, $"========================================================");
            return "";
        }


    }
}
