using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WahooFitToGarmin.Services;
using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IDropboxSettingsService _dropboxSettingsService;

        public UploadController(ILogger<UploadController> logger, IDropboxSettingsService dropboxSettingsServiceService)
        {
            _logger = logger;
            _dropboxSettingsService = dropboxSettingsServiceService;
            _logger.LogInformation($"DropboxAppName: {_dropboxSettingsService.GetDropboxAppName()}");
            _logger.LogInformation($"DropboxAppToken:{_dropboxSettingsService.GetDropboxAppToken()}");
            _logger.LogInformation($"DropboxAppSecret:{_dropboxSettingsService.GetDropboxAppSecret()}");
            _logger.LogInformation($"========================================================");
        }

        [Microsoft.AspNetCore.Mvc.HttpGet]
        public ActionResult Verify(string challenge)
        {
            _logger.LogInformation($"Enter Verify GET method");
            _logger.LogInformation($"challenge received : {challenge}");
            _logger.LogInformation($"========================================================");
            return Content(challenge);
        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        public async Task<ActionResult> GetNotification()
        {
            _logger.LogInformation($"Enter GetNotification POST method");
            // Receive a list of changed user IDs from Dropbox and process each.

            // Make sure this is a valid request from Dropbox
            // Get the request signature
            StringValues signatureHeader;
            Request.Headers.TryGetValue("X-Dropbox-Signature", out signatureHeader);
            _logger.LogInformation($"headerValueResult: {signatureHeader}");
            if (!signatureHeader.Any())
                return Forbid();

            // Get the signature value
            string signature = signatureHeader.FirstOrDefault();

            // Extract the raw body of the request
            string body = null;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                _logger.LogInformation($"dataReceived: {await reader.ReadToEndAsync()}");
                body = await reader.ReadToEndAsync();
            }

            // Check that the signature is good
            string appSecret = _dropboxSettingsService.GetDropboxAppSecret();
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                if (!VerifySha256Hash(hmac, body, signature))
                    return Forbid();
            }

            // Do your thing here... e.g. store it in a queue to process later
            // ...

            // Return A-OK :)
            return Ok();

        }
        private string GetSha256Hash(HMACSHA256 sha256Hash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            var stringBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            foreach (byte t in data)
            {
                stringBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string. 
            return stringBuilder.ToString();
        }

        private bool VerifySha256Hash(HMACSHA256 sha256Hash, string input, string hash)
        {
            // Hash the input. 
            string hashOfInput = GetSha256Hash(sha256Hash, input);

            if (String.Compare(hashOfInput, hash, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            return false;
        }

    }
}
