using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using FileCategorization_Api.Common;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Domain.Entities.DD_Web;

namespace FileCategorization_Api.Services;

/// <summary>
/// Service for DD web scraping operations with proper error handling and connection management
/// </summary>
public class DDWebScrapingService : IDDWebScrapingService
{
    private readonly ILogger<DDWebScrapingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly HttpClient _httpClient;

    private static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

    public DDWebScrapingService(
        ILogger<DDWebScrapingService> logger,
        IConfiguration configuration,
        IHostEnvironment environment,
        HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
        _httpClient = httpClient;
        
        // Configure HttpClient
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<Result<string>> GetPageContentAsync(string threadUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(threadUrl))
                return Result<string>.Failure("Thread URL cannot be null or empty");

            if (!Uri.TryCreate(threadUrl, UriKind.Absolute, out var uri))
                return Result<string>.Failure("Invalid thread URL format");

            var credentials = GetCredentials();
            if (!credentials.IsSuccess)
                return Result<string>.Failure($"Failed to get credentials: {credentials.ErrorMessage}");

            _logger.LogInformation("Attempting to access thread: {Url}", threadUrl);

            // First, check if already logged in
            var checkResponse = await _httpClient.GetAsync(uri, cancellationToken);
            if (!checkResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Initial request failed with status: {StatusCode}", checkResponse.StatusCode);
                return Result<string>.Failure($"Failed to access thread: HTTP {checkResponse.StatusCode}");
            }

            var checkContent = await checkResponse.Content.ReadAsStringAsync(cancellationToken);
            
            // Check if already logged in by looking for username in content
            if (checkContent.Contains(credentials.Data.Username, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Already logged in, accessing protected page");
                return Result<string>.Success(checkContent);
            }

            _logger.LogInformation("Not logged in, performing login");

            // Perform login
            var loginResult = await PerformLoginAsync(uri, credentials.Data, cancellationToken);
            if (!loginResult.IsSuccess)
                return Result<string>.Failure($"Login failed: {loginResult.ErrorMessage}");

            // Get the actual page content after login
            var pageResponse = await _httpClient.GetAsync(uri, cancellationToken);
            if (!pageResponse.IsSuccessStatusCode)
                return Result<string>.Failure($"Failed to access page after login: HTTP {pageResponse.StatusCode}");

            var pageContent = await pageResponse.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation("Successfully retrieved page content ({Length} characters)", pageContent.Length);
            return Result<string>.Success(pageContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for URL: {Url}", threadUrl);
            return Result<string>.Failure($"HTTP request failed: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout for URL: {Url}", threadUrl);
            return Result<string>.Failure("Request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error accessing URL: {Url}", threadUrl);
            return Result<string>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<DD_Threads>> ParseThreadInfoAsync(string htmlContent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return Result<DD_Threads>.Failure("HTML content cannot be null or empty");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Find page title from h3 with class="first"
            var h3Node = htmlDoc.DocumentNode.SelectSingleNode("//h3[@class='first']");
            if (h3Node == null)
            {
                _logger.LogWarning("No title h3 tag found with class 'first'");
                return Result<DD_Threads>.Failure("Unable to find thread title in page content");
            }

            var aNode = h3Node.SelectSingleNode("a");
            if (aNode == null)
            {
                _logger.LogWarning("No anchor tag found within title h3");
                return Result<DD_Threads>.Failure("Unable to extract thread title from page content");
            }

            var linkText = aNode.InnerText.Trim();
            if (string.IsNullOrWhiteSpace(linkText))
                return Result<DD_Threads>.Failure("Thread title is empty");

            var thread = new DD_Threads
            {
                MainTitle = WebUtility.HtmlDecode(linkText),
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _logger.LogInformation("Successfully parsed thread title: {Title}", thread.MainTitle);
            return Result<DD_Threads>.Success(thread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing thread information from HTML content");
            return Result<DD_Threads>.Failure($"Failed to parse thread info: {ex.Message}");
        }
    }

    public async Task<Result<List<DD_LinkEd2k>>> ParseEd2kLinksAsync(string htmlContent, DD_Threads thread, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return Result<List<DD_LinkEd2k>>.Failure("HTML content cannot be null or empty");

            if (thread == null)
                return Result<List<DD_LinkEd2k>>.Failure("Thread cannot be null");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Pattern to match ed2k:// or file:// links
            const string linkPattern = @"(?:ed2k:\/\/|file:\/\/)[^""'\s]+";
            var matches = Regex.Matches(htmlDoc.Text, linkPattern, RegexOptions.IgnoreCase);

            if (matches.Count == 0)
            {
                _logger.LogInformation("No ED2K or file links found in content");
                return Result<List<DD_LinkEd2k>>.Success(new List<DD_LinkEd2k>());
            }

            // Extract unique links
            var uniqueLinks = matches
                .Cast<Match>()
                .Where(m => m.Success)
                .Select(m => m.Value)
                .Distinct()
                .ToList();

            _logger.LogInformation("Found {Count} unique links", uniqueLinks.Count);

            var ed2kLinks = new List<DD_LinkEd2k>();
            var titleRegex = new Regex(@"\|file\|([^|]+)\|", RegexOptions.IgnoreCase);

            foreach (var link in uniqueLinks)
            {
                try
                {
                    var match = titleRegex.Match(link);
                    if (!match.Success)
                    {
                        _logger.LogDebug("Could not extract filename from link: {Link}", link);
                        continue;
                    }

                    var title = match.Groups[1].Value;
                    title = WebUtility.UrlDecode(title);
                    title = WebUtility.HtmlDecode(title);

                    var cleanLink = WebUtility.UrlDecode(link);
                    cleanLink = WebUtility.HtmlDecode(cleanLink);

                    var ddLink = new DD_LinkEd2k
                    {
                        Title = title,
                        Ed2kLink = cleanLink,
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                        IsUsed = false,
                        Threads = thread
                    };

                    ed2kLinks.Add(ddLink);
                    _logger.LogDebug("Parsed link: {Title}", title);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing individual link: {Link}", link);
                    continue;
                }
            }

            _logger.LogInformation("Successfully parsed {Count} ED2K links", ed2kLinks.Count);
            return Result<List<DD_LinkEd2k>>.Success(ed2kLinks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing ED2K links from HTML content");
            return Result<List<DD_LinkEd2k>>.Failure($"Failed to parse ED2K links: {ex.Message}");
        }
    }

    #region Private Methods

    private Result<(string Username, string Password)> GetCredentials()
    {
        try
        {
            string username, password;

            if (_environment.IsDevelopment())
            {
                username = _configuration["DD_USERNAME"];
                password = _configuration["DD_PSW"];
            }
            else
            {
                username = Environment.GetEnvironmentVariable("DD_USERNAME");
                password = Environment.GetEnvironmentVariable("DD_PSW");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Result<(string, string)>.Failure("DD credentials not configured");
            }

            return Result<(string, string)>.Success((username, password));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving DD credentials");
            return Result<(string, string)>.Failure($"Failed to get credentials: {ex.Message}");
        }
    }

    private async Task<Result<bool>> PerformLoginAsync(Uri threadUri, (string Username, string Password) credentials, CancellationToken cancellationToken)
    {
        try
        {
            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", credentials.Username),
                new KeyValuePair<string, string>("password", credentials.Password),
                new KeyValuePair<string, string>("redirect", "index.php"),
                new KeyValuePair<string, string>("login", "Login")
            });

            var loginResponse = await _httpClient.PostAsync(threadUri, loginData, cancellationToken);
            
            if (!loginResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Login failed with status: {StatusCode}", loginResponse.StatusCode);
                return Result<bool>.Failure($"Login request failed: HTTP {loginResponse.StatusCode}");
            }

            _logger.LogInformation("Login successful");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing login");
            return Result<bool>.Failure($"Login error: {ex.Message}");
        }
    }

    #endregion
}