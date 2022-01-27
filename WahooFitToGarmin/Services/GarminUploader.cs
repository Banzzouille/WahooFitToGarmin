using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WahooFitToGarmin.Helpers;

namespace WahooFitToGarmin.Services
{
    public class GarminUploader : IGarminUploader
    {
        public async Task UploadAsync(string garminLogin, string garminPwd, string filePath, ILogger logger)
        {
            logger.LogInformation($"{nameof(GarminUploader)}.{nameof(UploadAsync)}");

            logger.LogInformation("Connection to Garmin Connect server");
            var api = new ApiClient(garminLogin, garminPwd);
            var resultInitAuth = await api.InitAuth();
            logger.LogInformation(resultInitAuth);

            try
            {
                logger.LogInformation($"Uploading file {filePath}");
                var response = await api.UploadActivity(filePath).ConfigureAwait(false);

                var result = response.DetailedImportResult;

                if (result.Failures.Any())
                {
                    foreach (var failure in result.Failures)
                    {
                        if (failure.Messages.Any())
                        {
                            foreach (var message in failure.Messages)
                            {
                                if (message.Code == 202)
                                {
                                    logger.LogInformation($"Activity already uploaded {result.FileName}");
                                    System.IO.File.Delete(filePath);
                                }
                                else
                                {
                                    logger.LogInformation($"Failed to upload activity to Garmin. Message: {message}");
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(response.DetailedImportResult.UploadId))
                {
                    logger.LogInformation($"Successful upload for file {filePath}");
                    System.IO.File.Delete(filePath);
                }

            }
            catch (Exception e)
            {
                logger.LogInformation($"Failed to upload workout {filePath} : {e.Message}");
            }
        }
    }
}
