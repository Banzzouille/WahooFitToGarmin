using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WahooFitToGarmin.Services;
using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DropboxController : Controller
    {
        private readonly IGarminUploader _garminUploader;
        private readonly ILogger<DropboxController> _logger;
        private readonly IDropboxSettingsService _dropboxSettingsService;
        private readonly IGarminConnectSettingsService _garminConnectSettingsService;

        public DropboxController(IGarminUploader garminUploader, IDropboxSettingsService dropboxSettingsServiceService, IGarminConnectSettingsService garminConnectSettingsService,ILogger<DropboxController> logger)
        {
            _garminUploader = garminUploader;
            _logger = logger;
            _dropboxSettingsService = dropboxSettingsServiceService;
            _garminConnectSettingsService = garminConnectSettingsService;
            _logger.LogInformation($"{DateTime.Now} ==> DropboxAppName: {_dropboxSettingsService.GetDropboxAppName()}");
            _logger.LogInformation($"{DateTime.Now} ==> DropboxAppToken:{_dropboxSettingsService.GetDropboxAppToken()}");
            _logger.LogInformation($"{DateTime.Now} ==> DropboxAppSecret:{_dropboxSettingsService.GetDropboxAppSecret()}");
            _logger.LogInformation($"========================================================");
        }

        [HttpGet]
        public ActionResult Verify(string challenge)
        {
            _logger.LogInformation($"{DateTime.Now} ==> Enter Verify GET method");
            _logger.LogInformation($"{DateTime.Now} ==> challenge received : {challenge}");
            _logger.LogInformation($"========================================================");
            return Content(challenge);
        }

        [HttpPost]
        public async Task<ActionResult> GetNotification()
        {
            // Get the request signature
            StringValues signatureHeader;
            Request.Headers.TryGetValue("X-Dropbox-Signature", out signatureHeader);
            _logger.LogInformation($"{DateTime.Now} ==> headerValueResult: {signatureHeader}");
            if (!signatureHeader.Any())
                return Forbid();

            // Get the signature value
            string signature = signatureHeader.FirstOrDefault();

            // Extract the raw body of the request
            string body = null;
            using (StreamReader reader = new StreamReader(ControllerContext.HttpContext.Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            // Check that the signature is good
            string appSecret = _dropboxSettingsService.GetDropboxAppSecret();
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                if (!VerifySha256Hash(hmac, body, signature))
                    return BadRequest();
            }

            await DownLoadFitFiles(GetDropboxClient());

            // Return A-OK :)
            return Ok();
        }
    

        async Task DownLoadFitFiles(DropboxClient dbx)
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
                        var finalFileName = file.Name.Replace(" ", "-");
                        if (!System.IO.Directory.Exists("Activities"))
                            System.IO.Directory.CreateDirectory("Activities");
                        
                        if(Directory.GetFiles("Activities").FirstOrDefault(act=>act.Contains(finalFileName))!=null)
                            continue;

                        _logger.LogInformation($"{DateTime.Now} ==> downloading file: {file.PathLower}");

                        var fileContent = await response.GetContentAsByteArrayAsync();
                        await System.IO.File.WriteAllBytesAsync(Path.Combine("Activities", finalFileName), fileContent);

                        _logger.LogInformation($"{DateTime.Now} ==> deleting file: {file.PathLower}");
                        await dbx.Files.DeleteV2Async(file.PathLower);

                        await _garminUploader.UploadAsync(_garminConnectSettingsService.GetGarminConnectUserName(),
                            _garminConnectSettingsService.GetGarminConnectPassword(), Path.Combine("Activities", finalFileName), _logger).ConfigureAwait(false);
                    }
                }
            }
        }

        private DropboxClient GetDropboxClient()
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
