using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin.Services
{
    public interface IGarminUploader
    {
        Task UploadAsync(string garminLogin, string garminPwd, string filePath, ILogger logger);
    }
}
