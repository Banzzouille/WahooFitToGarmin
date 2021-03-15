using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IOptionsMonitor<DropboxSettings> _dropboxSettings;

        public UploadController(ILogger<UploadController> logger, IOptionsMonitor<DropboxSettings> dropboxSettings)
        {
            _logger = logger;
            _dropboxSettings = dropboxSettings;
            _logger.Log(LogLevel.Information, $"DropboxAppName: {_dropboxSettings.CurrentValue.DropboxAppName}   DropboxAppToken:{_dropboxSettings.CurrentValue.DropboxAppToken}");
        }

        [HttpGet]
        public ActionResult<string> Get(string challenge)
        {
            _logger.Log(LogLevel.Information,$"challenge received : {challenge}");
            return challenge;
        }

        
    }
}
