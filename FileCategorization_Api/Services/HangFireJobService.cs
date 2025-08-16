using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Shared.Enums;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
            var processedFiles = new List<(FileMoveDto moveDto, FilesDetail dbFile, string status, string message)>();
            var trainingDataEntries = new List<string>();

            var _originDir = await _configsService.GetConfigValue("ORIGINDIR");
            var _destDir = await _configsService.GetConfigValue("DESTDIR");

            // Batch load all files from database to avoid N+1 queries
            var fileIds = filesToMove.Select(f => f.Id).ToList();
            var dbFiles = await _context.FilesDetail
                .Where(f => fileIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            foreach (var file in filesToMove)
            {
                try
                {
                    if (!dbFiles.TryGetValue(file.Id, out var _file))
                    {
                        _logger.LogWarning($"File with id {file.Id} is not present.");
                        processedFiles.Add((file, null!, "IdNotPresent", $"fileName with id {file.Id} not present"));
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
                    
                    // Update database record
                    _file.FileCategory = file.FileCategory;
                    _file.IsToCategorize = false;
                    _file.IsNew = false;
                    _file.IsNotToMove = false;

                    // Collect training data entry for batch write
                    trainingDataEntries.Add($"{_file.Id};{_file.FileCategory};{_file.Name}");
                    
                    processedFiles.Add((file, _file, "Completed", $"fileName = {_file.Name} completed"));
                    _logger.LogInformation($"File {_file.Name} processed successfully");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Move File process failed: {e.Message}");
                    processedFiles.Add((file, null!, "Failed", $"Error in moving fileName with id {file.Id}: {e.Message}"));
                }
            }

            // Batch update database records
            var successfulFiles = processedFiles
                .Where(pf => pf.status == "Completed" && pf.dbFile != null)
                .Select(pf => pf.dbFile)
                .ToList();

            if (successfulFiles.Any())
            {
                try
                {
                    _context.FilesDetail.UpdateRange(successfulFiles);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation($"Batch updated {successfulFiles.Count} database records");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to batch update database: {ex.Message}");
                }
            }

            // Batch write training data
            if (trainingDataEntries.Any())
            {
                try
                {
                    var trainDataPath = await _configsService.GetConfigValue("TRAINDATAPATH");
                    var trainDataName = await _configsService.GetConfigValue("TRAINDATANAME");
                    var fullPath = Path.Combine(trainDataPath, trainDataName);
                    
                    await File.AppendAllLinesAsync(fullPath, trainingDataEntries, cancellationToken);
                    _logger.LogInformation($"Batch wrote {trainingDataEntries.Count} training data entries");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to batch write training data: {ex.Message}");
                }
            }

            // Send batch notifications
            try
            {
                foreach (var batch in processedFiles.Chunk(10)) // Send in chunks of 10
                {
                    foreach (var processedFile in batch)
                    {
                        var resultType = processedFile.status switch
                        {
                            "IdNotPresent" => MoveFilesResults.IdNotPresent,
                            "Completed" => MoveFilesResults.Completed,
                            "Failed" => MoveFilesResults.Failed,
                            _ => MoveFilesResults.Failed
                        };

                        await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", 
                            processedFile.dbFile?.Id ?? processedFile.moveDto.Id,
                            processedFile.message, resultType, cancellationToken);
                    }
                    
                    // Small delay between batches to avoid overwhelming clients
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send batch notifications: {ex.Message}");
            }

            var jobExecutionTime = await _utility.TimeDiff(jobStartTime, DateTime.Now);
            _logger.LogInformation($"Job Completed: [{jobExecutionTime}] - Moved {fileMoved} files");
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
                
                // Get all existing file names in one query to avoid N+1 queries
                var allFileNames = fileInfoList.Select(f => f.Name).ToList();
                var existingFileNames = (await _context.FilesDetail
                    .Where(fd => allFileNames.Contains(fd.Name))
                    .Select(fd => fd.Name)
                    .ToListAsync(cancellationToken))
                    .ToHashSet();

                // Process files in batches to reduce memory usage
                for (int i = 0; i < fileInfoList.Count; i += batchSize)
                {
                    var batch = fileInfoList.Skip(i).Take(batchSize);
                    var batchFilesToAdd = new List<FilesDetail>();
                    
                    foreach (FileInfo file in batch)
                    {
                        totalFilesInFolder += 1;

                        if (!existingFileNames.Contains(file.Name))
                        {
                            _logger.LogInformation($"file {file.Name} not present in db");

                            batchFilesToAdd.Add(new FilesDetail
                            {
                                Name = file.Name,
                                FileSize = file.Length,
                                LastUpdateFile = file.LastWriteTime,
                                Path = file.Directory?.FullName ?? _origDir,
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
                        var categorizedFilesResult = await _machineLearningService.PredictFileCategoriesAsync(batchFilesToAdd);

                        if (categorizedFilesResult.IsFailure)
                        {
                            _logger.LogError("Failed to categorize files in batch {BatchNumber}: {ErrorMessage}", 
                                i/batchSize + 1, categorizedFilesResult.Error);
                            continue; // Skip this batch and continue with next
                        }
                        
                        var _categorizedFiles = categorizedFilesResult.Value!;
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