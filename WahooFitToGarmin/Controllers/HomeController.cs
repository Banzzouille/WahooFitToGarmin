using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dropbox.Api;
using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConvertController : ControllerBase
    {
        private readonly ILogger<ConvertController> _logger;

        public ConvertController(ILogger<ConvertController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello world !";
        }

        [HttpGet("NewFile")]
        public string NewFile()
        {
            return "NewFile !";
        }

        static async Task Run(DropboxClient dbx)
        {
            var full = await dbx.Users.GetCurrentAccountAsync();
            Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
        }

        static async Task ListRootFolder(DropboxClient dbx)
        {
            var list = await dbx.Files.ListFolderAsync(string.Empty, true, true);

            // show folders then files
            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                Console.WriteLine("D  {0}/", item.Name);

                var files = await dbx.Files.ListFolderAsync(item.PathLower);
                foreach (var file in files.Entries.Where(i => i.IsFile))
                {
                    Console.WriteLine("F{0,8} {1}", file.AsFile.Size, file.Name);
                }
            }
        }

        static DropboxClient GetClient()
        {
            var currentClient = new DropboxClient(
                 "*", new DropboxClientConfig("WahooFitToGarmin"));

            return currentClient;
        }

    }
}
