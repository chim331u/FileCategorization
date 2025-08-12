using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Api.Domain.Enums;
using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace FileCategorization_Api.Services
{
    public class HangFireJobService : IHangFireJobService
    {
        private readonly ApplicationContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<HangFireJobService> _logger;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IFilesDetailService _serviceData;
        private readonly IConfigsService _configsService;
        private readonly IUtilityServices _utility;
        private readonly IMachineLearningService _machineLearningService;

        public HangFireJobService(ILogger<HangFireJobService> logger, ApplicationContext context, IConfiguration config,
            IHubContext<NotificationHub> hubContext, IFilesDetailService serviceData, IUtilityServices utility,
            IConfigsService configsService, IMachineLearningService machineLearningService)

        {
            _context = context;
            _config = config;
            _logger = logger;
            _notificationHub = hubContext;
            _serviceData = serviceData;
            _utility = utility;
            _configsService = configsService;
            _machineLearningService = machineLearningService;
        }

        /// This method is called by Hangfire to test signlalR
        public async Task ExecuteAsync(string fileName, string destinationFolder, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting background job");
            await _notificationHub.Clients.All.SendAsync("notifications", "Starting background job", 0);
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await _notificationHub.Clients.All.SendAsync("notifications", $"fileName = {fileName}", 0);
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await _notificationHub.Clients.All.SendAsync("notifications", $"destinationFolder = {destinationFolder}",
                0);
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            _logger.LogInformation("Completed background job");

            await _notificationHub.Clients.All.SendAsync("notifications", "Completed processing job", 1);
        }

        public async Task MoveFilesJob(List<FileMoveDto> filesToMove, CancellationToken cancellationToken)
        {
            var jobStartTime = DateTime.Now;
            int fileMoved = 0;

            var _originDir = await _configsService.GetConfigValue("ORIGINDIR");
            var _destDir = await _configsService.GetConfigValue("DESTDIR");

            foreach (var file in filesToMove)
            {
                try
                {
                    var _file = await _context.FilesDetail.FindAsync(file.Id);

                    if (_file == null)
                    {
                        _logger.LogWarning($"File with id {file.Id} is not present.");
                        await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", file.FileCategory,
                            $"fileName with id {file.Id} not present", MoveFilesResults.IdNotPresent);
                        continue;
                    }

                    var fileOrigin = Path.Combine(_originDir, _file.Name);
                    var folderDest = Path.Combine(_destDir, file.FileCategory);
                    var fileDest = Path.Combine(folderDest, _file.Name);

                    // Determine whether the directory exists.
                    if (!Directory.Exists(folderDest))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(folderDest);
                        _logger.LogInformation($"Destination folder {folderDest} created");
                    }

                    // Move the file.
                    File.Move(fileOrigin, fileDest);
                    _logger.LogInformation($"{fileOrigin} moved to {fileDest}.");
                    fileMoved++;
                    await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", _file.Id,
                        $"fileName = {_file.Name} moved", MoveFilesResults.Moved);
                    //update db
                    _file.FileCategory = file.FileCategory;

                    _file.IsToCategorize = false;
                    _file.IsNew = false;
                    _file.IsNotToMove = false;

                    var result = await _serviceData.UpdateFilesDetail(_file);
                    _logger.LogInformation($"Db updated: File {_file.Name}");

                    //add train data
                    using (StreamWriter sw = File.AppendText(Path.Combine(
                               _configsService.GetConfigValue("TRAINDATAPATH").Result,
                               _configsService.GetConfigValue("TRAINDATANAME").Result)))
                    {
                        sw.WriteLine(_file.Id + ";" + _file.FileCategory + ";" + _file.Name);
                    }

                    _logger.LogInformation($"File: {_file.Name} added to train model file");
                    await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", _file.Id,
                        $"fileName = {_file.Name} completed", MoveFilesResults.Completed);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Move File process failed: {e.Message}");
                    await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", file.Id,
                        $"Error in moving fileName with id {file.Id}: {e.Message}", MoveFilesResults.Failed);
                }
            }

            var jobExecutionTime =await _utility.TimeDiff(jobStartTime, DateTime.Now);
            _logger.LogInformation($"Job Completed: [{jobExecutionTime}]");
            await _notificationHub.Clients.All.SendAsync("jobNotifications",
                $"Completed Job in [{jobExecutionTime}] - Moved {fileMoved} files", MoveFilesResults.Completed);
        }

        public async Task RefreshFiles(CancellationToken cancellationToken)
        {
            var jobStartTime = DateTime.Now;
            await _notificationHub.Clients.All.SendAsync("refreshFilesNotifications", $"Started refresh Files", 0);

            _logger.LogInformation("Start RefreshFile process");

            var _origDir = await _configsService.GetConfigValue("ORIGINDIR");

            int totalFilesInFolder = 0;
            int fileAdded = 0;

            DirectoryInfo dirFilesInfo = new DirectoryInfo(_origDir);

            var allFilesInOrigDir = dirFilesInfo.GetFileSystemInfos();

            var filesInOrigDir = allFilesInOrigDir.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden));


            try
            {
                const int batchSize = 100;
                var filesToAddToDb = new List<FilesDetail>();
                var fileInfoList = filesInOrigDir.Cast<FileInfo>().ToList();
                
                // Process files in batches to reduce memory usage
                for (int i = 0; i < fileInfoList.Count; i += batchSize)
                {
                    var batch = fileInfoList.Skip(i).Take(batchSize);
                    var batchFilesToAdd = new List<FilesDetail>();
                    
                    foreach (FileInfo file in batch)
                    {
                        totalFilesInFolder += 1;

                        if (!_serviceData.FileNameIsPresent(file.Name).Result)
                        {
                            _logger.LogInformation($"file {file.Name} not present in db");

                            batchFilesToAdd.Add(new FilesDetail
                            {
                                Name = file.Name,
                                FileSize = file.Length,
                                LastUpdateFile = file.LastWriteTime,
                                Path = file.Directory.FullName,
                                IsToCategorize = true,
                                IsNew = true
                            });
                        }
                    }

                    if (batchFilesToAdd.Count > 0)
                    {
                        //calculate category for this batch
                        _logger.LogInformation($"Start Prediction process for batch {i/batchSize + 1}");
                        var _categorizationStartTime = DateTime.Now;
                        var _categorizedFiles = _machineLearningService.PredictFileCategorization(batchFilesToAdd);

                        _logger.LogInformation(
                            $"End Prediction process for batch: [{await _utility.TimeDiff(_categorizationStartTime, DateTime.Now)}]");

                        //add batch to db
                        _logger.LogInformation("Start add process for batch");
                        var _addStartTime = DateTime.Now;
                        var batchAdded = await _serviceData.AddFileDetailList(_categorizedFiles);
                        fileAdded += batchAdded;

                        _logger.LogInformation($"End adding process for batch: [{await _utility.TimeDiff(_addStartTime, DateTime.Now)}]");
                        
                        // Send progress notification
                        await _notificationHub.Clients.All.SendAsync("refreshFilesNotifications", 
                            $"Processed batch {i/batchSize + 1}, added {batchAdded} files", 0);
                    }
                }
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error in refresh files :{ex.Message}");
                await _notificationHub.Clients.All.SendAsync("refreshFilesNotifications",
                    $"Error in refresh files :{ex.Message}", 0);
            }

            _logger.LogInformation($"End RefreshFile process: [{await _utility.TimeDiff(jobStartTime, DateTime.Now)}]");

            var jobExecutionTime = await _utility.TimeDiff(jobStartTime, DateTime.Now);
            _logger.LogInformation($"Refresh Files Job Completed: [{jobExecutionTime}]");
            await _notificationHub.Clients.All.SendAsync("jobNotifications",
                $"Refresh Files job Completed in [{jobExecutionTime}] - Added {fileAdded} files, total files in folder: {totalFilesInFolder}",
                0);
        }

    }
}