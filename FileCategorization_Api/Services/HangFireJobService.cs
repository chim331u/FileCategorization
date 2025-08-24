using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Shared.Enums;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;

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
        private readonly IActionsRepository _actionsRepository;

        public HangFireJobService(ILogger<HangFireJobService> logger, ApplicationContext context, IConfiguration config,
            IHubContext<NotificationHub> hubContext, IFilesDetailService serviceData, IUtilityServices utility,
            IConfigsService configsService, IMachineLearningService machineLearningService, IActionsRepository actionsRepository)

        {
            _context = context;
            _config = config;
            _logger = logger;
            _notificationHub = hubContext;
            _serviceData = serviceData;
            _utility = utility;
            _configsService = configsService;
            _machineLearningService = machineLearningService;
            _actionsRepository = actionsRepository;
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
            int fileFailed = 0;
            var processedFiles = new List<(FileMoveDto moveDto, FilesDetail dbFile, string status, string message)>();
            var trainingDataEntries = new List<string>();

            try
            {
                // 1. Messaggio iniziale - Job preso in carico con numero totale file
                _logger.LogInformation("Starting MoveFiles job for {TotalFiles} files", filesToMove.Count);
                await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", 
                    $"📂 Starting file movement job for {filesToMove.Count} files...", 
                    MoveFilesResults.Processing, cancellationToken);

                // Validate configuration paths
                var _originDir = await _configsService.GetConfigValue("ORIGINDIR");
                var _destDir = await _configsService.GetConfigValue("DESTDIR");
                
                if (string.IsNullOrEmpty(_originDir))
                {
                    var errorMsg = "ORIGINDIR configuration is missing or empty";
                    _logger.LogError(errorMsg);
                    await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                        JsonSerializer.Serialize(new {
                            success = false,
                            message = "Move files job failed: " + errorMsg,
                            totalFiles = filesToMove.Count,
                            movedFiles = 0,
                            failedFiles = filesToMove.Count,
                            duration = TimeSpan.Zero.ToString(),
                            completedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                        }), MoveFilesResults.Failed, filesToMove.Count, 0, filesToMove.Count, cancellationToken);
                    return;
                }
                
                if (string.IsNullOrEmpty(_destDir))
                {
                    var errorMsg = "DESTDIR configuration is missing or empty";
                    _logger.LogError(errorMsg);
                    await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                        JsonSerializer.Serialize(new {
                            success = false,
                            message = "Move files job failed: " + errorMsg,
                            totalFiles = filesToMove.Count,
                            movedFiles = 0,
                            failedFiles = filesToMove.Count,
                            duration = TimeSpan.Zero.ToString(),
                            completedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                        }), MoveFilesResults.Failed, filesToMove.Count, 0, filesToMove.Count, cancellationToken);
                    return;
                }

                // Batch load all files from database to avoid N+1 queries
                var fileIds = filesToMove.Select(f => f.Id).ToList();
                var dbFiles = await _context.FilesDetail
                    .Where(f => fileIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id, cancellationToken);

                // Process each file with individual notifications
                foreach (var file in filesToMove)
                {
                    var fileStartTime = DateTime.Now;
                    try
                    {
                        if (!dbFiles.TryGetValue(file.Id, out var _file))
                        {
                            fileFailed++;
                            var errorMsg = $"File with id {file.Id} not found in database";
                            _logger.LogWarning(errorMsg);
                            processedFiles.Add((file, null!, "IdNotPresent", errorMsg));
                            
                            // 2. Messaggio per singolo file - FAIL
                            await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", 
                                file.Id, "Unknown File", "", errorMsg,
                                MoveFilesResults.IdNotPresent, cancellationToken);
                            continue;
                        }

                        var fileOrigin = Path.Combine(_originDir, _file.Name);
                        var folderDest = Path.Combine(_destDir, file.FileCategory);
                        var fileDest = Path.Combine(folderDest, _file.Name);

                        // Check if source file exists
                        if (!File.Exists(fileOrigin))
                        {
                            fileFailed++;
                            var errorMsg = $"Source file not found: {fileOrigin}";
                            _logger.LogError(errorMsg);
                            processedFiles.Add((file, _file, "Failed", errorMsg));
                            
                            // 2. Messaggio per singolo file - FAIL
                            await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", 
                                file.Id, _file.Name, "", "Source file not found",
                                MoveFilesResults.Failed, cancellationToken);
                            continue;
                        }

                        // Create destination directory if it doesn't exist
                        if (!Directory.Exists(folderDest))
                        {
                            Directory.CreateDirectory(folderDest);
                            _logger.LogInformation($"Created destination folder: {folderDest}");
                        }

                        // Move the file
                        File.Move(fileOrigin, fileDest);
                        var fileProcessTime = DateTime.Now - fileStartTime;
                        fileMoved++;
                        
                        // Update database record
                        _file.FileCategory = file.FileCategory;
                        _file.IsToCategorize = false;
                        _file.IsNew = false;
                        _file.IsNotToMove = false;

                        // Collect training data entry for batch write
                        trainingDataEntries.Add($"{_file.Id};{_file.FileCategory};{_file.Name}");
                        
                        processedFiles.Add((file, _file, "Completed", $"File {_file.Name} moved successfully"));
                        _logger.LogInformation($"File {_file.Name} moved to {file.FileCategory} in {fileProcessTime.TotalMilliseconds:F0}ms");

                        // 2. Messaggio per singolo file - SUCCESS
                        var destinationPath = Path.Combine(_destDir, file.FileCategory);
                        await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", 
                            file.Id, _file.Name, destinationPath, $"Moved successfully in {fileProcessTime.TotalMilliseconds:F0}ms",
                            MoveFilesResults.Completed, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        fileFailed++;
                        var fileProcessTime = DateTime.Now - fileStartTime;
                        var errorMsg = $"Error moving file with id {file.Id}: {e.Message}";
                        _logger.LogError(e, errorMsg);
                        processedFiles.Add((file, dbFiles.GetValueOrDefault(file.Id), "Failed", errorMsg));
                        
                        // 2. Messaggio per singolo file - FAIL con errore
                        var fileName = dbFiles.GetValueOrDefault(file.Id)?.Name ?? $"Unknown File";
                        await _notificationHub.Clients.All.SendAsync("moveFilesNotifications", 
                            file.Id, fileName, "", $"{e.Message} ({fileProcessTime.TotalMilliseconds:F0}ms)",
                            MoveFilesResults.Failed, cancellationToken);
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
                        _logger.LogError(ex, "Failed to batch update database: {Message}", ex.Message);
                    }
                }

                // Batch write training data
                if (trainingDataEntries.Any())
                {
                    try
                    {
                        var trainDataPath = await _configsService.GetConfigValue("TRAINDATAPATH");
                        var trainDataName = await _configsService.GetConfigValue("TRAINDATANAME");
                        
                        if (!string.IsNullOrEmpty(trainDataPath) && !string.IsNullOrEmpty(trainDataName))
                        {
                            var fullPath = Path.Combine(trainDataPath, trainDataName);
                            await File.AppendAllLinesAsync(fullPath, trainingDataEntries, cancellationToken);
                            _logger.LogInformation($"Batch wrote {trainingDataEntries.Count} training data entries");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to batch write training data: {Message}", ex.Message);
                    }
                }

                // Calculate execution time
                var jobExecutionTime = DateTime.Now - jobStartTime;
                var executionTimeString = jobExecutionTime.TotalSeconds < 60 
                    ? $"{jobExecutionTime.TotalSeconds:F1}s" 
                    : $"{jobExecutionTime.TotalMinutes:F1}m";

                // 3. Messaggio finale - Job completato con resoconto
                var isSuccess = fileFailed == 0;
                var completionMessage = JsonSerializer.Serialize(new
                {
                    success = isSuccess,
                    message = isSuccess 
                        ? $"Move files job completed successfully in {executionTimeString}"
                        : $"Move files job completed with errors in {executionTimeString}",
                    totalFiles = filesToMove.Count,
                    movedFiles = fileMoved,
                    failedFiles = fileFailed,
                    duration = executionTimeString,
                    completedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                });

                _logger.LogInformation("MoveFiles job completed - Total: {Total}, Moved: {Moved}, Failed: {Failed}, Duration: {Duration}", 
                    filesToMove.Count, fileMoved, fileFailed, executionTimeString);

                // 3. Invio messaggio finale completamento job
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    completionMessage, 
                    isSuccess ? MoveFilesResults.Completed : MoveFilesResults.Failed, 
                    filesToMove.Count, fileMoved, fileFailed,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Job fallito completamente
                var jobExecutionTime = DateTime.Now - jobStartTime;
                var executionTimeString = $"{jobExecutionTime.TotalSeconds:F1}s";
                
                var errorMessage = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = $"Move files job failed: {ex.Message}",
                    totalFiles = filesToMove.Count,
                    movedFiles = fileMoved,
                    failedFiles = filesToMove.Count - fileMoved,
                    duration = executionTimeString,
                    error = ex.Message,
                    completedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                });

                _logger.LogError(ex, "MoveFiles job failed after {Duration}", executionTimeString);
                
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    errorMessage, 
                    MoveFilesResults.Failed, 
                    filesToMove.Count, fileMoved, fileFailed,
                    cancellationToken);
            }
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

        /// <summary>
        /// Background job for force categorization of uncategorized files with progress tracking.
        /// Executes ML categorization for files that haven't been categorized yet and provides detailed feedback via SignalR.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for job cancellation</param>
        public async Task ForceCategorizeJob(CancellationToken cancellationToken)
        {
            var jobStartTime = DateTime.Now;
            _logger.LogInformation("Starting force categorization background job");

            try
            {
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    "Starting force categorization of uncategorized files...", 0, cancellationToken);

                // Get uncategorized files
                _logger.LogInformation("Retrieving uncategorized files");
                var uncategorizedFilesResult = await _actionsRepository.GetUncategorizedFilesAsync(cancellationToken);
                
                if (uncategorizedFilesResult.IsFailure)
                {
                    var errorMsg = $"Failed to retrieve uncategorized files: {uncategorizedFilesResult.Error}";
                    _logger.LogError(errorMsg);
                    await _notificationHub.Clients.All.SendAsync("jobNotifications", errorMsg, MoveFilesResults.Failed, cancellationToken);
                    return;
                }

                var uncategorizedFiles = uncategorizedFilesResult.Value ?? new List<FilesDetail>();
                var totalFiles = uncategorizedFiles.Count;
                
                if (totalFiles == 0)
                {
                    var noFilesMsg = "No uncategorized files found to process";
                    _logger.LogInformation(noFilesMsg);
                    await _notificationHub.Clients.All.SendAsync("jobNotifications", noFilesMsg, MoveFilesResults.Processing, cancellationToken);
                    return;
                }

                _logger.LogInformation("Found {TotalFiles} uncategorized files to process", totalFiles);
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    $"Found {totalFiles} uncategorized files to categorize", 0, cancellationToken);

                // Process files in batches for performance
                var batchSize = 50; // Process in smaller batches for better responsiveness
                var categorizedCount = 0;
                var errorCount = 0;

                for (int i = 0; i < totalFiles; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchFiles = uncategorizedFiles.Skip(i).Take(batchSize).ToList();
                    var batchNumber = (i / batchSize) + 1;
                    var totalBatches = (int)Math.Ceiling((double)totalFiles / batchSize);

                    _logger.LogInformation("Processing batch {BatchNumber}/{TotalBatches} ({BatchSize} files)", 
                        batchNumber, totalBatches, batchFiles.Count);

                    await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                        $"Processing batch {batchNumber}/{totalBatches} ({batchFiles.Count} files)...", 0, cancellationToken);

                    try
                    {
                        // Categorize this batch
                        var categorizationResult = await _machineLearningService.PredictFileCategoriesAsync(batchFiles, cancellationToken);
                        
                        if (categorizationResult.IsFailure)
                        {
                            errorCount += batchFiles.Count;
                            _logger.LogError("Failed to categorize batch {BatchNumber}: {Error}", batchNumber, categorizationResult.Error);
                            await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                                $"Error categorizing batch {batchNumber}: {categorizationResult.Error}", 0, cancellationToken);
                            continue;
                        }

                        var categorizedFiles = categorizationResult.Value!;
                        
                        // Update files in database
                        var updateResult = await _actionsRepository.UpdateFilesBatchAsync(categorizedFiles, cancellationToken);
                        
                        if (updateResult.IsFailure)
                        {
                            errorCount += batchFiles.Count;
                            _logger.LogError("Failed to update batch {BatchNumber} in database: {Error}", batchNumber, updateResult.Error);
                            await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                                $"Error updating batch {batchNumber}: {updateResult.Error}", 0, cancellationToken);
                            continue;
                        }

                        categorizedCount += batchFiles.Count;
                        var progress = (int)((double)(i + batchFiles.Count) / totalFiles * 100);
                        
                        _logger.LogInformation("Successfully categorized batch {BatchNumber}, progress: {Progress}%", 
                            batchNumber, progress);
                        
                        await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                            $"Categorized batch {batchNumber}/{totalBatches} - Progress: {progress}%", progress, cancellationToken);
                    }
                    catch (Exception batchEx)
                    {
                        errorCount += batchFiles.Count;
                        _logger.LogError(batchEx, "Exception processing batch {BatchNumber}", batchNumber);
                        await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                            $"Exception in batch {batchNumber}: {batchEx.Message}", 0, cancellationToken);
                    }
                }

                var jobExecutionTime = DateTime.Now - jobStartTime;
                var successMessage = JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Force categorization completed successfully",
                    totalFiles = totalFiles,
                    categorizedFiles = categorizedCount,
                    errorFiles = errorCount,
                    duration = jobExecutionTime,
                    completedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Force categorization job completed - Total: {Total}, Success: {Success}, Errors: {Errors}, Duration: {Duration}", 
                    totalFiles, categorizedCount, errorCount, jobExecutionTime);

                await _notificationHub.Clients.All.SendAsync("jobNotifications", successMessage, MoveFilesResults.Completed, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var cancelledMessage = "Force categorization job was cancelled";
                _logger.LogWarning(cancelledMessage);
                await _notificationHub.Clients.All.SendAsync("jobNotifications", cancelledMessage, MoveFilesResults.Failed, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var jobExecutionTime = DateTime.Now - jobStartTime;
                var errorMessage = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Force categorization job failed",
                    error = ex.Message,
                    duration = jobExecutionTime,
                    failedAt = DateTime.UtcNow
                });

                _logger.LogError(ex, "Force categorization job failed after {Duration}", jobExecutionTime);
                await _notificationHub.Clients.All.SendAsync("jobNotifications", errorMessage, MoveFilesResults.Failed, cancellationToken);
            }
        }

        /// <summary>
        /// Background job for machine learning model training with comprehensive progress tracking.
        /// Executes ML model training asynchronously and provides detailed feedback via SignalR.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for job cancellation</param>
        public async Task TrainModelJob(CancellationToken cancellationToken)
        {
            var jobStartTime = DateTime.Now;
            _logger.LogInformation("HangfireJobService.TrainModelJob() STARTED");
            _logger.LogInformation("Starting ML model training background job");
            
            try
            {
                // Notify job start
                _logger.LogInformation("🔔 DEBUG: Sending SignalR jobNotifications - Starting training");
                _logger.LogInformation("🔔 DEBUG: Message content: 'Starting machine learning model training...', Result: {Result}", MoveFilesResults.Processing);
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    "Starting machine learning model training...", MoveFilesResults.Processing);
                _logger.LogInformation("🔔 DEBUG: SignalR message sent successfully");

                // Execute model training
                _logger.LogInformation("Executing ML model training");
                var trainResult = await _machineLearningService.TrainAndSaveModelAsync(cancellationToken);

                if (trainResult.IsFailure)
                {
                    _logger.LogError("ML model training failed: {Error}", trainResult.Error);
                    await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                        $"❌ Model training failed: {trainResult.Error}", MoveFilesResults.Failed);
                    return;
                }

                var jobExecutionTime = await _utility.TimeDiff(jobStartTime, DateTime.Now);
                _logger.LogInformation("ML model training completed successfully in {Duration}", jobExecutionTime);

                // Get additional model information for detailed feedback
                var modelInfoResult = await _machineLearningService.GetModelInfoAsync(cancellationToken);
                var modelInfo = modelInfoResult.IsSuccess ? modelInfoResult.Value : "Model information unavailable";

                // Prepare detailed success message with JSON structure for parsing
                var successMessage = new
                {
                    success = true,
                    message = $"Model training completed successfully. Training Duration: {jobExecutionTime} - Model Version: {DateTime.UtcNow:yyyyMMddHHmmss}",
                    trainingDuration = jobExecutionTime,
                    modelVersion = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                    modelInfo = modelInfo,
                    completedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss")
                };

                // Send structured success notification (will be parsed by frontend GlobalConsole)
                _logger.LogInformation("🔔 DEBUG: Sending SignalR jobNotifications - Training completed");
                var serializedMessage = System.Text.Json.JsonSerializer.Serialize(successMessage);
                _logger.LogInformation("🔔 DEBUG: Serialized message: {SerializedMessage}", serializedMessage);
                _logger.LogInformation("🔔 DEBUG: Result enum: {Result}", MoveFilesResults.Completed);
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    serializedMessage, MoveFilesResults.Completed);
                _logger.LogInformation("🔔 DEBUG: SignalR completion message sent successfully");

                _logger.LogInformation("ML model training job completed successfully: [{JobExecutionTime}]", jobExecutionTime);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ML model training job was cancelled");
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    "⚠️ Model training was cancelled", MoveFilesResults.Failed);
            }
            catch (Exception ex)
            {
                var jobExecutionTime = await _utility.TimeDiff(jobStartTime, DateTime.Now);
                _logger.LogError(ex, "ML model training job failed after {Duration}: {ErrorMessage}", 
                    jobExecutionTime, ex.Message);
                
                await _notificationHub.Clients.All.SendAsync("jobNotifications", 
                    $"❌ Model training failed after {jobExecutionTime}: {ex.Message}", MoveFilesResults.Failed);
            }
        }

    }
}