using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public ActionResult<string> Get(string challenge)
        {
            _logger.Log(LogLevel.Information,$"challenge received : {challenge}");
            return challenge;
        }

        [HttpPost]
        public ActionResult GetNotification([FromBody] string dataReceive)
        {
            _logger.Log(LogLevel.Information, $"data received : {dataReceive}");
            return Ok();
        }


    }
}
