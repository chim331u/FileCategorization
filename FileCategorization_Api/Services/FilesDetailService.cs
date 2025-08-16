using FileCategorization_Api.Domain.Entities.FilesDetail;
using FileCategorization_Api.Infrastructure.Data;
using FileCategorization_Shared.DTOs.FileManagement;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.FileCategorization;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Services
{
    /// <summary>
    /// Files Detail Services
    /// </summary>
    public class FilesDetailService : IFilesDetailService
    {
        private readonly ApplicationContext _context; // Database context
        private readonly ILogger<FilesDetailService> _logger; // Logger for logging information and error
        private readonly IUtilityServices _utilityServices;
        private readonly IMachineLearningService _machineLearningService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilesDetailService"/> class.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="logger">Logger for logging information and errors.</param>
        /// <param name="utilityServices">Utility services for additional operations.</param>
        /// <param name="machineLearningService">Machine learning service for predictions.</param>
        public FilesDetailService(ApplicationContext context, ILogger<FilesDetailService> logger, IUtilityServices utilityServices, IMachineLearningService machineLearningService)
        {
            _context = context;
            _logger = logger;
            _utilityServices = utilityServices;
            _machineLearningService = machineLearningService;
        }
        
        /// <summary>
        /// Get the list of Categories
        /// </summary>
        /// <returns>Category list ordered by name</returns>
        public async Task<List<string?>> GetDbCategoryList()
        {
            try
            {
                return await _context.FilesDetail.AsNoTracking().Select(c => c.FileCategory)
                    .Distinct().OrderBy(a => a)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }
        }

        /// <summary>
        /// Get the list of files
        /// </summary>
        /// <returns>List af all active files</returns>
        public async Task<IEnumerable<FilesDetailResponse>> GetFileList()
        {
            try
            {
                return await _context.FilesDetail.AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(f => new FilesDetailResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        FileCategory = f.FileCategory,
                        IsToCategorize = f.IsToCategorize,
                        IsNew = f.IsNew,
                        FileSize = f.FileSize,
                        IsNotToMove = f.IsNotToMove
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        /// <summary>
        /// Get all files by category
        /// </summary>
        /// <param name="fileCategory"></param>
        /// <returns>All active files by category</returns>
        public async Task<IEnumerable<FilesDetailResponse?>> GetAllFiles(string fileCategory)
        {
            try
            {
                return await _context.FilesDetail.AsNoTracking()
                    .Where(x => x.IsNotToMove == false && x.FileCategory == fileCategory)
                    .OrderByDescending(x => x.Name)
                    .Select(f => new FilesDetailResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        FileCategory = f.FileCategory,
                        IsToCategorize = f.IsToCategorize,
                        IsNew = f.IsNew,
                        FileSize = f.FileSize,
                        IsNotToMove = f.IsNotToMove
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error return file list for category {ex.Message}");
                return null!;
            }
        }

        /// <summary>
        /// Get a filed detail by its ID
        /// </summary>
        /// <param name="id">ID of the filesDetail</param>
        /// <returns>Details of the filesDetail</returns>
        public async Task<FilesDetailResponse?> GetFilesDetailById(int id)
        {
            try
            {
                // Find the files detail by its ID
                var filesDetail = await _context.FilesDetail.FindAsync(id);
                if (filesDetail == null)
                {
                    _logger.LogWarning($"Files Detail with ID {id} not found.");
                    return null!;
                }

                // Return the details of the filesDetail
                return new FilesDetailResponse()
                {
                    Id = filesDetail.Id, Name = filesDetail.Name, Path = filesDetail.Path,
                    FileCategory = filesDetail.FileCategory,
                    FileSize = filesDetail.FileSize, IsNotToMove = filesDetail.IsNotToMove,
                    IsToCategorize = filesDetail.IsToCategorize, IsNew = filesDetail.IsNew
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving filesDetail: {ex.Message}");
                return null!;
            }
        }

        public async Task<IEnumerable<FilesDetailResponse>> GetFileListToCategorize()
        {
            try
            {
                return await _context.FilesDetail.AsNoTracking()
                    .Where(x => x.IsActive && x.IsToCategorize)
                    .OrderBy(x => x.Name)
                    .Select(f => new FilesDetailResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        FileCategory = f.FileCategory,
                        IsToCategorize = f.IsToCategorize,
                        IsNew = f.IsNew,
                        FileSize = f.FileSize,
                        IsNotToMove = f.IsNotToMove
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        
        public async Task<IEnumerable<FilesDetail>> GetFileListModelToCategorize()
        {
            try
            {
                var filesDetail =await _context.FilesDetail.OrderBy(x => x.Name).Where(x => x.IsActive && x.IsToCategorize)
                    .ToListAsync();

                // Return the details of the filesDetail
                // Return the details of all files
                return filesDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return null;
            }
        }
        
        /// <summary>
        /// Add new File detail
        /// </summary>
        /// <param name="FilesDetailRequest"></param>
        /// <returns>Details of the created book</returns>
        public async Task<FilesDetailResponse?> AddFileDetailAsync(FilesDetailRequest filesDetailRequest)
        {
            try
            {
                var filesDetail = new FilesDetail
                {
                    CreatedDate = DateTime.Now,
                    FileCategory = filesDetailRequest.FileCategory,
                    FileSize = filesDetailRequest.FileSize,
                    IsActive = true, IsDeleted = false, IsNew = true, IsNotToMove = false, IsToCategorize = true,
                    Name = filesDetailRequest.Name, Path = filesDetailRequest.Path
                };

                // Add the book to the database
                _context.FilesDetail.Add(filesDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Files detail added successfully.");

                // Return the details of the created book
                return new FilesDetailResponse
                {
                    Id = filesDetail.Id, FileCategory = filesDetail.FileCategory, FileSize = filesDetail.FileSize,
                    IsNotToMove = filesDetail.IsNotToMove, IsToCategorize = filesDetail.IsToCategorize,
                    IsNew = filesDetail.IsNew,
                    Name = filesDetail.Name, Path = filesDetail.Path
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding files detail: {ex.Message}");
                return null!;
            }
        }

        public async Task<int> AddFileDetailList(List<FilesDetail> fileDetail)
        {
            try
            {
                int count = 0;
                foreach (var item in fileDetail)
                {

                    item.CreatedDate = DateTime.Now;
                    item.IsActive = true;
                    item.IsDeleted = false;
                    item.IsNew = true;
                    item.IsNotToMove = false;
                    item.LastUpdatedDate = DateTime.Now;
                    item.IsActive = true;
                    item.CreatedDate = DateTime.Now;
                    
                    var fileAdded = await _context.FilesDetail.AddAsync(item);
                    count++;
                }

                await _context.SaveChangesAsync();

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return 0;
            }
        }
        /// <summary>
        /// Update an existing files detail
        /// </summary>
        /// <param name="id">ID of the files detail to be updated</param>
        /// <param name="filesDetail">Updated files detail model</param>
        /// <returns>Details of the updated files detail</returns>
        public async Task<FilesDetailResponse?> UpdateFilesDetailAsync(int id, FilesDetailUpdateRequest filesDetail)
        {
            try
            {
                // Find the existing files detail by its ID
                var existingFilesDetail = await _context.FilesDetail.FindAsync(id);
                if (existingFilesDetail == null)
                {
                    _logger.LogWarning($"Files Detail with ID {id} not found.");
                    return null;
                }

                // Update the files details
                existingFilesDetail.FileCategory = filesDetail.FileCategory;

                if (filesDetail.FileSize != null) existingFilesDetail.FileSize = filesDetail.FileSize;
                if (filesDetail.IsNotToMove != null) existingFilesDetail.IsNotToMove = filesDetail.IsNotToMove;
                if (filesDetail.IsToCategorize != null) existingFilesDetail.IsToCategorize = filesDetail.IsToCategorize;
                if (filesDetail.FileCategory != null) existingFilesDetail.FileCategory = filesDetail.FileCategory;
                if (filesDetail.IsNew != null) existingFilesDetail.IsNew = filesDetail.IsNew;
                if (filesDetail.Name != null) existingFilesDetail.Name = filesDetail.Name;
                if (filesDetail.Path != null) existingFilesDetail.Path = filesDetail.Path;
                if (filesDetail.IsDeleted != null) existingFilesDetail.IsDeleted = filesDetail.IsDeleted;
                if (filesDetail.LastUpdateFile != null) existingFilesDetail.LastUpdateFile = filesDetail.LastUpdateFile;
                existingFilesDetail.LastUpdatedDate = DateTime.Now;


                // Save the changes to the database
                _context.FilesDetail.Update(existingFilesDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Files Detail updated successfully.");

                // Return the files details of the updated filesdetail
                return new FilesDetailResponse
                {
                    Id = existingFilesDetail.Id,
                    Name = existingFilesDetail.Name,
                    Path = existingFilesDetail.Path,
                    FileCategory = existingFilesDetail.FileCategory,
                    IsToCategorize = existingFilesDetail.IsToCategorize,
                    IsNew = existingFilesDetail.IsNew,
                    FileSize = existingFilesDetail.FileSize,
                    IsNotToMove = existingFilesDetail.IsNotToMove
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating files detail: {ex.Message}");
                return null!;
            }
        }
        
        public async Task UpdateFileDetailList(List<FilesDetail> fileDetail)
        {
            try
            {
                foreach (var item in fileDetail)
                {
                    item.LastUpdatedDate = DateTime.Now;
                    _context.FilesDetail.Update(item);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Files updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

            }

        }
              
        public async Task<FilesDetail?> UpdateFilesDetail(FilesDetail filesDetail)
        {
            try
            {
                // Find the existing files detail by its ID
                var existingFilesDetail = await _context.FilesDetail.FindAsync(filesDetail.Id);
                if (existingFilesDetail == null)
                {
                    _logger.LogWarning($"Files Detail with ID {filesDetail.Id} not found.");
                    return null;
                }

                // Update the files details
               filesDetail.LastUpdatedDate = DateTime.Now;


                // Save the changes to the database
                _context.FilesDetail.Update(filesDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Files Detail updated successfully.");

                // Return the files details of the updated filesdetail
                return filesDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating files detail: {ex.Message}");
                return null!;
            }
        }
        /// <summary>
        /// Delete an existing files detail
        /// </summary>
        /// <param name="id">ID of the files detail to be deleted</param>
        /// <returns>Details of the updated files detail</returns>
        public async Task<bool> DeleteFilesDetailAsync(int id)
        {
            try
            {
                // Find the existing files detail by its ID
                var existingFilesDetail = await _context.FilesDetail.FindAsync(id);
                if (existingFilesDetail == null)
                {
                    _logger.LogWarning($"Files Detail with ID {id} not found.");
                    return false;
                }

                // Update the files details
                existingFilesDetail.IsActive = false;
                existingFilesDetail.LastUpdatedDate = DateTime.Now;
                
                // Save the changes to the database
                _context.FilesDetail.Update(existingFilesDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Files Detail deleted successfully.");

                // Return the files details of the deleted filesdetail
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting files detail: {ex.Message}");
                return false!;
            }
        }
        
        /// <summary>
        /// Get the last view list of files
        /// </summary>
        /// <returns>list of FilesDetail</returns>
        public async Task<IEnumerable<FilesDetailResponse?>> GetLastViewList()
        {
            try
            {
                // Load all eligible files into memory first, then group and process
                var allFiles = await _context.FilesDetail.AsNoTracking()
                    .Where(x => x.IsNotToMove == false && x.FileCategory != null)
                    .ToListAsync();

                // Group by category and get the latest file for each category (in memory)
                var latestFiles = allFiles
                    .GroupBy(x => x.FileCategory)
                    .Select(g => g.OrderByDescending(x => x.Name).First())
                    .OrderBy(x => x.FileCategory);

                // Map to response DTO
                return latestFiles.Select(f => new FilesDetailResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    FileCategory = f.FileCategory,
                    IsToCategorize = f.IsToCategorize,
                    IsNew = f.IsNew,
                    FileSize = f.FileSize,
                    IsNotToMove = f.IsNotToMove
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null!;
            }
        }

        public async Task<string?> ForceCategory()
        {
            _logger.LogInformation("Categorizing file ...");

            try
            {
                var _filesToCategorize = (await GetFileListModelToCategorize()).ToList();

                if (_filesToCategorize != null && _filesToCategorize.Count > 0)
                {
                    //calculate category
                    _logger.LogInformation("Start Prediction process");
                    var _categorizationStartTime = DateTime.Now;
                    var categorizedFilesResult = await _machineLearningService.PredictFileCategoriesAsync(_filesToCategorize);

                    if (categorizedFilesResult.IsFailure)
                    {
                        _logger.LogError("Failed to categorize files: {ErrorMessage}", categorizedFilesResult.Error);
                        return null;
                    }
                    
                    var _categorizedFiles = categorizedFilesResult.Value!;
                    _logger.LogInformation($"End Prediction process: [{_utilityServices.TimeDiff(_categorizationStartTime, DateTime.Now)}]");

                    //add to db
                    _logger.LogInformation("Start add process");
                    var _addStartTime = DateTime.Now;
                    await UpdateFileDetailList(_categorizedFiles);

                    _logger.LogInformation($"End adding process: [{_utilityServices.TimeDiff(_addStartTime, DateTime.Now)}]");
                }

                _logger.LogInformation("DB Updated: All File categorized.");
                return "DB Updated: All File categorized.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error force category: {ex.Message}");
                return $"Error force category: {ex.Message}";
            }
          
        }
        public async Task<bool> FileNameIsPresent(string fileName)
        {
            try
            {
                var fileDetailList = await _context.FilesDetail.Where(x => x.Name == fileName).AnyAsync();

                return fileDetailList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return false;
            }
        }
    }
}