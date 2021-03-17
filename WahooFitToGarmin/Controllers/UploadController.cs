using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
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
            _logger.LogInformation($"{DateTime.Now} ==> DropboxAppName: {_dropboxSettingsService.GetDropboxAppName()}");
            _logger.LogInformation($"{DateTime.Now} ==> DropboxAppToken:{_dropboxSettingsService.GetDropboxAppToken()}");
            _logger.LogInformation($"{DateTime.Now} ==> DropboxAppSecret:{_dropboxSettingsService.GetDropboxAppSecret()}");
            _logger.LogInformation($"========================================================");
        }

        [Microsoft.AspNetCore.Mvc.HttpGet]
        public ActionResult Verify(string challenge)
        {
            _logger.LogInformation($"{DateTime.Now} ==> Enter Verify GET method");
            _logger.LogInformation($"{DateTime.Now} ==> challenge received : {challenge}");
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
            _logger.LogInformation($"{DateTime.Now} ==> headerValueResult: {signatureHeader}");
            if (!signatureHeader.Any())
                return Forbid();

            // Get the signature value
            string signature = signatureHeader.FirstOrDefault();

            // Extract the raw body of the request
            string body = await new StreamReader(ControllerContext.HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInformation($"{DateTime.Now} ==> dataReceived: {body}");

            // Check that the signature is good
            string appSecret = _dropboxSettingsService.GetDropboxAppSecret();
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                if (!VerifySha256Hash(hmac, body, signature))
                    return Forbid();
            }

            await ListRootFolder(GetClient());

            return Ok();

        }

        async Task ListRootFolder(DropboxClient dbx)
        {
            var list = await dbx.Files.ListFolderAsync(string.Empty, true, true);

            // show folders then files
            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                if(!item.Name.Equals("WahooFitness")) continue;
                Console.WriteLine("D  {0}/", item.Name);

                var files = await dbx.Files.ListFolderAsync(item.PathLower);
                foreach (var file in files.Entries.Where(i => i.IsFile))
                {
                    if(!file.Name.EndsWith(".fit")) continue;
                    Console.WriteLine("F{0,8} {1}", file.AsFile.Size, file.Name);
                    using (var response = await dbx.Files.DownloadAsync(file.PathLower))
                    {
                        if (!System.IO.Directory.Exists("Activities"))
                            System.IO.Directory.CreateDirectory("Activities");

                        if (!System.IO.File.Exists(Path.Combine("Activities", file.Name)))
                            System.IO.File.Delete(Path.Combine("Activities", file.Name));

                        _logger.LogInformation($"{DateTime.Now} ==> downloading file: {file.PathLower}");
                        var fileContent = await response.GetContentAsByteArrayAsync();
                        await System.IO.File.WriteAllBytesAsync(Path.Combine("Activities", file.Name), fileContent);

                        //_logger.LogInformation($"{DateTime.Now} ==> deleting file: {file.PathLower}");
                        //await dbx.Files.DeleteV2Async(file.PathLower);
                    }
                }
            }
        }

        private DropboxClient GetClient()
        {
            var currentClient = new DropboxClient(
                _dropboxSettingsService.GetDropboxAppToken(), new DropboxClientConfig(_dropboxSettingsService.GetDropboxAppName()));

            return currentClient;
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
