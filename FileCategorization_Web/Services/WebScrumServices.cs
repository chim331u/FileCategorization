using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileCategorization_Web.Data.DTOs.WebScrum;
using FileCategorization_Web.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace FileCategorization_Web.Services;

public class WebScrumServices : IWebScrumServices
{
    HttpClient _httpClient;
    JsonSerializerOptions _serializerOptions;
    private readonly IConfiguration _config;

    public WebScrumServices(IConfiguration config)
    {
        _httpClient = new HttpClient();

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            NumberHandling =
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.WriteAsString,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        _config = config;
    }

    public async Task<List<ThreadsDto>> GetActiveThreads()
    {
        Console.WriteLine($"Request active threads");

        Uri uri = new Uri(string.Format(GetRestUrl() + $"api/v1/GetActiveThreads", string.Empty));

        try
        {
            var threadsList = await _httpClient.GetFromJsonAsync<List<ThreadsDto>>(uri);

            if (threadsList != null)
            {
                Console.WriteLine($"Received threads list");
            }
            else
            {
                Console.WriteLine($"Threads list not received");
            }

            return threadsList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.InnerException}");

            return null;
        }
    }

    public async Task<List<Ed2kLinkDto>> GetEd2kLinks(int threadId)
    {
        Console.WriteLine($"Request links for thread i = {threadId}");

        Uri uri = new Uri(string.Format(GetRestUrl() + $"api/v1/GetLinks/{threadId}", string.Empty));

        try
        {
            var linkList = await _httpClient.GetFromJsonAsync<List<Ed2kLinkDto>>(uri);

            if (linkList != null)
            {
                Console.WriteLine($"Received link list");
            }
            else
            {
                Console.WriteLine($"Link list not received");
            }

            return linkList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.InnerException}");

            return null;
        }
    }

    public async Task<string> UseLink(int linkId)
    {
        Console.WriteLine($"Request use link id = {linkId}");

        Uri uri = new Uri(string.Format(GetRestUrl() + $"api/v1/UseLink/{linkId}", string.Empty));

        try
        {
            var result = await _httpClient.GetFromJsonAsync<string>(uri);

            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine($" Link Used");
                return result;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.InnerException}");

            return string.Empty;
        }
    }

    public async Task<bool> RenewThread(int threadId)
    {
        Console.WriteLine($"Request check url for thread id= {threadId}");

        Uri uri = new Uri(string.Format(GetRestUrl() + $"api/v1/CheckLinks/{threadId}/", string.Empty));

        try
        {
            var result = await _httpClient.GetAsync(uri);

            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine($"{result.StatusCode.ToString()} - Url Checked");
                return result.IsSuccessStatusCode;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.InnerException}");

            return false;
        }
    }

    public async Task<bool> CheckUrl(string urlToCheck)
    {
        Console.WriteLine($"Request check url = {urlToCheck}");

        var _urlToCheck = Base64UrlEncoder.Encode(urlToCheck);

        Uri uri = new Uri(string.Format(GetRestUrl() + $"api/v1/CheckLink/{_urlToCheck}/", string.Empty));

        try
        {
            var result = await _httpClient.GetAsync(uri);

            if (result.IsSuccessStatusCode)
            {
                Console.WriteLine($"{result.StatusCode.ToString()} - Url Checked");
                return result.IsSuccessStatusCode;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.Message} - {ex.InnerException}");

            return false;
        }
    }
    public string GetRestUrl()
    {
        var uri = _config.GetSection("Uri").Value;
        return uri;
    }
}