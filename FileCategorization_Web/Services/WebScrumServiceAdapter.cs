using FileCategorization_Web.Data.DTOs.WebScrum;
using FileCategorization_Web.Interfaces;

namespace FileCategorization_Web.Services;

/// <summary>
/// Adapter service that provides backward compatibility for WebScrum services.
/// Automatically selects between modern v2 and legacy v1 implementations based on configuration.
/// </summary>
public class WebScrumServiceAdapter : IWebScrumServices
{
    private readonly IWebScrumServices _implementation;
    private readonly ILogger<WebScrumServiceAdapter> _logger;

    public WebScrumServiceAdapter(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<WebScrumServiceAdapter> logger)
    {
        _logger = logger;

        // Check if modern FileCategorizationApi configuration exists
        var modernConfig = configuration.GetSection("FileCategorizationApi:BaseUrl").Value;
        
        if (!string.IsNullOrEmpty(modernConfig))
        {
            _logger.LogInformation("Using modern WebScrum service with v2 API endpoints");
            _implementation = serviceProvider.GetRequiredService<ModernWebScrumService>();
        }
        else
        {
            _logger.LogInformation("Using legacy WebScrum service with v1 API endpoints");
            _implementation = serviceProvider.GetRequiredService<WebScrumServices>();
        }
    }

    public async Task<List<ThreadsDto>> GetActiveThreads()
    {
        return await _implementation.GetActiveThreads();
    }

    public async Task<List<Ed2kLinkDto>> GetEd2kLinks(int threadId)
    {
        return await _implementation.GetEd2kLinks(threadId);
    }

    public async Task<string> UseLink(int linkId)
    {
        return await _implementation.UseLink(linkId);
    }

    public async Task<bool> RenewThread(int threadId)
    {
        return await _implementation.RenewThread(threadId);
    }

    public async Task<bool> CheckUrl(string urlToCheck)
    {
        return await _implementation.CheckUrl(urlToCheck);
    }
}