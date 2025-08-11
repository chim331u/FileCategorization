using System.Net;
using System.Text.RegularExpressions;
using FileCategorization_Api.AppContext;
using FileCategorization_Api.Contracts.DD;
using FileCategorization_Api.Interfaces;
using FileCategorization_Api.Models.DD_Web;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

namespace FileCategorization_Api.Services;

/// <summary>
/// Provides services for managing DD threads, links, and settings.
/// </summary>
public class DDService : IDDService
{
    private readonly ApplicationContext _context; // Database context
    private readonly ILogger<DDService> _logger; // Logger for logging information and errors
    private readonly IConfiguration _config; // Configuration settings
    private readonly IUtilityServices _utility; // Utility class for common operations
    private readonly IHostEnvironment _environment;
   
    /// <summary>
    /// Initializes a new instance of the <see cref="DDService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    /// <param name="config">The configuration settings.</param>
    /// <param name="utility">The utility service for common operations.</param>
    /// <param name="environment">The host environment.</param>
    public DDService(ApplicationContext context, ILogger<DDService> logger, IConfiguration config, IUtilityServices utility, IHostEnvironment environment)
    {
        _utility = utility;
        _context = context;
        _logger = logger;
        _config = config;
        _environment = environment;
    }

    /// <summary>
    /// Retrieves links from a given URL and processes them.
    /// </summary>
    /// <param name="link">The URL to retrieve links from.</param>
    /// <returns>A string indicating the result of the operation.</returns>
    public async Task<string> GetLinks(string link)
    {
        var pageContent = await LoginDD(link);

        if (!string.IsNullOrEmpty(pageContent))
        {
            //crea o aggiorna thread
            var thread = await GetThread(pageContent);

            thread.Url = link;
            var oldThread = await _context.DDThreads.FirstOrDefaultAsync(x => x.Url.Equals(thread.Url));

            if (oldThread != null)
            {
                //already exists : update 
                oldThread.MainTitle = thread.MainTitle;
                oldThread.Type = thread.Type;
                oldThread.Note = thread.Note;
                thread = await UpdateThread(oldThread);
                _logger.LogInformation($"Thread already exists. Updated.");
            }
            else
            {
                //add
                thread = await AddThread(thread);
                _logger.LogInformation($"New thread: added.");
            }

            //cerca links
            List<DD_LinkEd2k>? links = await GetEd2kLinks(pageContent, thread);

            if (links != null)
            {
                //retrive all links for thread
                var savedLinks = await _context.DDLinkEd2.Where(x => x.Threads.Id == thread.Id).ToListAsync();

                List<DD_LinkEd2k> _linkToUpdate = new List<DD_LinkEd2k>();
                List<DD_LinkEd2k> _linkToAdd = new List<DD_LinkEd2k>();

                foreach (var _link in links)
                {
                    //check if link already exist
                    if (savedLinks.Any(x => x.Ed2kLink.Equals(_link.Ed2kLink)))
                    {
                        var oldLink = savedLinks.FirstOrDefault(x => x.Ed2kLink.Equals(_link.Ed2kLink));

                        // if (oldLink != null && oldLink.IsNew)
                        // {
                        //already exists : update isnew
                        oldLink.IsNew = false;
                        _linkToUpdate.Add(oldLink);
                        _logger.LogInformation($"Link: {_link.Title} updated.");
                        // await UpdateLinkEd2k(oldLink);
                        // }
                    }
                    else
                    {
                        //add
                        _link.IsNew = true;
                        _linkToAdd.Add(_link);
                        // await AddLinkEd2k(_link);
                        _logger.LogInformation($"New link: {_link.Title} added.");
                    }
                }

                if (_linkToUpdate.Count > 0)
                {
                    await UpdateListLinkEd2k(_linkToUpdate);
                }

                if (_linkToAdd.Count > 0)
                {
                    await AddListLinkEd2k(_linkToAdd);
                }
            }
        }

        return "ok";
    }

    /// <summary>
    /// Retrieves links for a specific thread by its ID.
    /// </summary>
    /// <param name="threadId">The ID of the thread.</param>
    /// <returns>A string indicating the result of the operation.</returns>
    public async Task<string> GetLinks(int threadId)
    {
        // var _setting = new NasSettings()
        //  {
        //      CreatedDate = DateTime.Now,
        //      IsActive = true,
        //      LastUpdatedDate = DateTime.Now,
        //      Id = 1, UserName = "chim", Password = "aneurysm"
        //  };
        // _setting = await AddSetting(_setting);

        var link = await _context.DDThreads.Where(x => x.Id == threadId).Select(x => x.Url).FirstOrDefaultAsync();


        if (link != null)
        {
            var pageContent = await LoginDD(link);

            if (!string.IsNullOrEmpty(pageContent))
            {
                //crea o aggiorna thread
                var thread = await GetThread(pageContent);

                thread.Url = link;
                var oldThread = await _context.DDThreads.FirstOrDefaultAsync(x => x.Url.Equals(thread.Url));

                if (oldThread != null)
                {
                    //already exists : update 

                    oldThread.MainTitle = thread.MainTitle;
                    oldThread.Type = thread.Type;
                    oldThread.Note = thread.Note;
                    thread = await UpdateThread(oldThread);
                    _logger.LogInformation($"Thread already exists. Updated.");
                }
                else
                {
                    //add
                    thread = await AddThread(thread);
                    _logger.LogInformation($"New thread: added.");
                }

                //cerca links
                List<DD_LinkEd2k>? links = await GetEd2kLinks(pageContent, thread);

                if (links != null)
                {
                    //retrive all links for thread
                    var savedLinks = await _context.DDLinkEd2.Where(x => x.Threads.Id == thread.Id).ToListAsync();

                    List<DD_LinkEd2k> _linkToUpdate = new List<DD_LinkEd2k>();
                    List<DD_LinkEd2k> _linkToAdd = new List<DD_LinkEd2k>();

                    foreach (var _link in links)
                    {
                        //check if exist
                        if (savedLinks.Any(x => x.Ed2kLink.Equals(_link.Ed2kLink)))
                        {
                            var oldLink = savedLinks.FirstOrDefault(x => x.Ed2kLink.Equals(_link.Ed2kLink));

                            // if (oldLink != null && oldLink.IsNew)
                            // {
                            //already exists : update isnew
                            oldLink.IsNew = false;
                            _linkToUpdate.Add(oldLink);
                            _logger.LogInformation($"Link: {_link.Title} updated.");
                            // await UpdateLinkEd2k(oldLink);
                            // }
                        }
                        else
                        {
                            //add
                            _link.IsNew = true;
                            _linkToAdd.Add(_link);
                            // await AddLinkEd2k(_link);
                            _logger.LogInformation($"New link: {_link.Title} added.");
                        }
                    }

                    if (_linkToUpdate.Count > 0)
                    {
                        await UpdateListLinkEd2k(_linkToUpdate);
                    }

                    if (_linkToAdd.Count > 0)
                    {
                        await AddListLinkEd2k(_linkToAdd);
                    }
                }
            }

            return "ok";
        }

        _logger.LogWarning("Thread not found.");
        return "Unable to find thread";
    }

    /// <summary>
    /// Marks a link as used by its ID.
    /// </summary>
    /// <param name="linkId">The ID of the link to mark as used.</param>
    /// <returns>A string indicating the result of the operation.</returns>
    public async Task<string> UseLink(int linkId)
    {
        //TODO : Actions to use the link --> send to mule

        var link = await _context.DDLinkEd2.FirstOrDefaultAsync(x => x.Id == linkId);

        if (link != null)
        {
            link.IsUsed = true;
            link.LastUpdatedDate = DateTime.Now;
            link.IsNew = false;

            var linkUpdated = await UpdateLinkEd2k(link);

            if (linkUpdated != null)
            {
                return link.Ed2kLink;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Retrieves all active threads.
    /// </summary>
    /// <returns>A list of active threads as <see cref="ThreadsDto"/> objects.</returns>
    public async Task<List<ThreadsDto>> GetActiveThreads()
    {
        try
        {
            var threads = await _context.DDThreads.Where(x => x.IsActive).ToListAsync();

//             List<ThreadsDto> threadsDtoList = new List<ThreadsDto>();

            var threadsDtoList = threads.Select(thread => new ThreadsDto
            {
                Id = thread.Id,
                MainTitle = thread.MainTitle,
                NewLinks = _context.DDLinkEd2.Any(x => x.Threads.Id == thread.Id),
                LinksNumber = _context.DDLinkEd2.Count(x => x.Id == thread.Id),
                Type = thread.Type
            });

            //
            // foreach (var _thread in threads)
            // {
            //     var links = await _context.DDLinkEd2.Where(x => x.Threads.Id == _thread.Id).ToListAsync();
            //     var dto = new ThreadsDto();
            //     dto.Id = _thread.Id;
            //     dto.MainTitle = _thread.MainTitle;
            //     dto.LinksNumber = links.Count;
            //     dto.NewLinks = links.Any(x => x.IsNew);
            //
            //     threadsDtoList.Add(dto);
            // }

            return threadsDtoList.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Retrieves all active links for a specific thread.
    /// </summary>
    /// <param name="threadId">The ID of the thread.</param>
    /// <returns>A list of active links as <see cref="Ed2kLinkDto"/> objects.</returns>
    public async Task<List<Ed2kLinkDto>> GetActiveLinks(int threadId)
    {
        try
        {
            var links = await _context.DDLinkEd2.Where(x => x.IsActive && x.Threads.Id == threadId).ToListAsync();

            return links.Select(x => new Ed2kLinkDto
            {
                Id = x.Id,
                Ed2kLink = x.Ed2kLink,
                IsNew = x.IsNew, 
                IsUsed = x.IsUsed, 
                ThreadId = threadId, 
                Title = x.Title
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Logs in to the DD website and retrieves the page content.
    /// </summary>
    /// <param name="threadLink">The URL of the thread to log in to.</param>
    /// <returns>The page content as a string.</returns>
    private async Task<string> LoginDD(string threadLink)
    {
        var _ddUserName = String.Empty;
        var _ddPassword = string.Empty;
        
        if (_environment.IsDevelopment())
        {
            //for debug only
            _ddUserName = _config["DD_USERNAME"];
            
        }
        else
        {
            _ddUserName = Environment.GetEnvironmentVariable("DD_USERNAME");
        }
        if (_environment.IsDevelopment())
        {
            //for debug only
            _ddPassword = _config["DD_PSW"];
            
        }
        else
        {
            _ddPassword = Environment.GetEnvironmentVariable("DD_PSW");
        }    
            var clientHandler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            using var client = new HttpClient(clientHandler);

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            // 1. Verifica se l'utente è già loggato
            var checkResponse = await client.GetAsync(threadLink);
            var checkContent = await checkResponse.Content.ReadAsStringAsync();

            if (checkContent
                .Contains(_ddUserName)) // Controlla se il testo della pagina indica la necessità di login
            {
                _logger.LogInformation("Sei già loggato! Accedendo alla pagina protetta...");
            }
            else
            {
                _logger.LogInformation("Non sei loggato. Procedo con il login...");

                // Parametri per il login
                var loginData = new Dictionary<string, string>
                {
                    { "username", _ddUserName }, // Nome utente
                    //{ "password", await _utility.DecryptString(_setting.Password) }, // Password
                    { "password", _ddPassword }, // Password
                    { "redirect", "index.php" },
                    { "login", "Login" } // Nome del pulsante di login (può variare)
                };

                var loginContent = new FormUrlEncodedContent(loginData);

                // Eseguire la richiesta POST per il login
                var response = await client.PostAsync(threadLink, loginContent);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Errore durante il login.");
                    return string.Empty;
                }

                _logger.LogInformation("Login effettuato con successo!");
            }

            // Accedere alla pagina richiesta
            var pageResponse = await client.GetAsync(threadLink);
            var pageContent = await pageResponse.Content.ReadAsStringAsync();

            return pageContent;

    }

    /// <summary>
    /// Parses the page content to extract the thread information.
    /// </summary>
    /// <param name="pageContent">The HTML content of the page.</param>
    /// <returns>A <see cref="DD_Threads"/> object representing the thread.</returns>
    private async Task<DD_Threads> GetThread(string pageContent)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(pageContent);

        //TROVA TITOLO PAGINA
        // Cerca il tag <h3> con class="first"

        string linkText = "";
        var h3Node = htmlDoc.DocumentNode.SelectSingleNode("//h3[@class='first']");
        if (h3Node == null)
        {
            _logger.LogWarning($"Nessun tag 'titolo' trovato.");
        }
        else
        {
            // All'interno di <h3 class="first">, cerca il tag <a>
            var aNode = h3Node.SelectSingleNode("a");
            if (aNode == null)
            {
                _logger.LogWarning($"Nessun tag 'titolo' trovato.");
                // return $"Nessun tag <a> trovato dentro <h3 class='first'>.";
            }
            else
            {
                // Estrae il testo e il valore dell'attributo href del tag <a>
                linkText = aNode.InnerText.Trim();
            }
        }

        //cread dd_thread
        var _thread = new DD_Threads
        {
            MainTitle = linkText,
            CreatedDate = DateTime.Now,
            IsActive = true
        };
        // Stampa i risultati
        _logger.LogInformation($"Titolo Pagina= {_thread.MainTitle}");
        return _thread;
    }

    /// <summary>
    /// Parses the page content to extract ed2k links.
    /// </summary>
    /// <param name="pageContent">The HTML content of the page.</param>
    /// <param name="thread">The thread associated with the links.</param>
    /// <returns>A list of <see cref="DD_LinkEd2k"/> objects representing the links.</returns>
    private async Task<List<DD_LinkEd2k>> GetEd2kLinks(string pageContent, DD_Threads thread)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(pageContent);
        // Definisce il pattern per catturare i link che iniziano per "ed2k://" oppure "file://"
        string pattern = @"(?:ed2k:\/\/|file:\/\/)[^""'\s]+";

        // Esegue la ricerca con Regex
        var matches = Regex.Matches(htmlDoc.Text, pattern);

        if (matches.Count == 0)
        {
            _logger.LogWarning("Nessun link trovato che inizi per 'ed2k://' o 'file://'.");
            return null;
        }

        // Usa un HashSet per evitare duplicati
        List<string> allLinks = new List<string>();
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                allLinks.Add(match.Value);
            }
        }

        var uniqueLinks = allLinks.Distinct().ToList();
        var _ed2kLinksList = new List<DD_LinkEd2k>();

        Regex regex = new Regex(@"\|file\|([^|]+)\|");

        foreach (var _link in uniqueLinks)
        {
            Match match = regex.Match(_link);

            if (match.Success)
            {
                var _title = match.Groups[1].Value;
                _title = WebUtility.UrlDecode(_title);
                _title = WebUtility.HtmlDecode(_title);
                var link = _link;
                link = WebUtility.UrlDecode(link);
                link = WebUtility.HtmlDecode(link);

                var dd_link = new DD_LinkEd2k
                {
                    Title = _title,
                    Ed2kLink = link,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    IsUsed = false,
                    Threads = thread
                };

                _ed2kLinksList.Add(dd_link);
            }
            else
            {
                _logger.LogInformation("Nome del file non trovato.");
            }
        }

        foreach (var ddLink in _ed2kLinksList)
        {
            _logger.LogInformation($"Titolo: {ddLink.Title} - Link: {ddLink.Ed2kLink}");
        }

        return _ed2kLinksList;
    }

    /// <summary>
    /// Adds a new thread to the database.
    /// </summary>
    /// <param name="thread">The thread to add.</param>
    /// <returns>The added thread as a <see cref="DD_Threads"/> object.</returns>
    public async Task<DD_Threads> AddThread(DD_Threads thread)
    {
        try
        {
            thread.CreatedDate = DateTime.Now;
            thread.IsActive = true;

            var threadAdded = await _context.DDThreads.AddAsync(thread);
            await _context.SaveChangesAsync();

            return threadAdded.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Updates an existing thread in the database.
    /// </summary>
    /// <param name="thread">The thread to update.</param>
    /// <returns>The updated thread as a <see cref="DD_Threads"/> object.</returns>
    public async Task<DD_Threads> UpdateThread(DD_Threads thread)
    {
        try
        {
            thread.LastUpdatedDate = DateTime.Now;

            var threadUpdated = _context.DDThreads.Update(thread);
            await _context.SaveChangesAsync();

            return threadUpdated.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Marks a thread as inactive in the database.
    /// </summary>
    /// <param name="thread">The thread to delete.</param>
    /// <returns>The deleted thread as a <see cref="DD_Threads"/> object.</returns>
    public async Task<DD_Threads> DeleteThread(DD_Threads thread)
    {
        try
        {
            thread.IsActive = false;
            thread.LastUpdatedDate = DateTime.Now;
            var threadDeleted = _context.DDThreads.Update(thread);
            await _context.SaveChangesAsync();

            return threadDeleted.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Adds a new link to the database.
    /// </summary>
    /// <param name="linkEd2K">The link to add.</param>
    /// <returns>The added link as a <see cref="DD_LinkEd2k"/> object.</returns>
    public async Task<DD_LinkEd2k> AddLinkEd2k(DD_LinkEd2k linkEd2K)
    {
        try
        {
            linkEd2K.CreatedDate = DateTime.Now;
            linkEd2K.IsActive = true;
            var linkAdded = await _context.DDLinkEd2.AddAsync(linkEd2K);
            await _context.SaveChangesAsync();

            return linkAdded.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Adds a list of links to the database.
    /// </summary>
    /// <param name="linkEd2KList">The list of links to add.</param>
    /// <returns>The added links as a list of <see cref="DD_LinkEd2k"/> objects.</returns>
    public async Task<List<DD_LinkEd2k>> AddListLinkEd2k(List<DD_LinkEd2k> linkEd2KList)
    {
        foreach (var linkEd2K in linkEd2KList)
        {
            try
            {
                linkEd2K.CreatedDate = DateTime.Now;
                linkEd2K.IsActive = true;
                var linkAdded = await _context.DDLinkEd2.AddAsync(linkEd2K);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error add link {linkEd2K.Title} - {ex.Message}");
                //return null;
            }
        }

        await _context.SaveChangesAsync();
        return linkEd2KList;
    }

    /// <summary>
    /// Updates an existing link in the database.
    /// </summary>
    /// <param name="linkEd2K">The link to update.</param>
    /// <returns>The updated link as a <see cref="DD_LinkEd2k"/> object.</returns>
    public async Task<DD_LinkEd2k> UpdateLinkEd2k(DD_LinkEd2k linkEd2K)
    {
        try
        {
            linkEd2K.LastUpdatedDate = DateTime.Now;
            var linkUpdated = _context.DDLinkEd2.Update(linkEd2K);
            await _context.SaveChangesAsync();

            return linkUpdated.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Updates a list of links in the database.
    /// </summary>
    /// <param name="linkEd2KList">The list of links to update.</param>
    /// <returns>The updated links as a list of <see cref="DD_LinkEd2k"/> objects.</returns>
    public async Task<List<DD_LinkEd2k>> UpdateListLinkEd2k(List<DD_LinkEd2k> linkEd2KList)
    {
        foreach (var linkEd2K in linkEd2KList)
        {
            try
            {
                linkEd2K.LastUpdatedDate = DateTime.Now;
                var linkUpdated = _context.DDLinkEd2.Update(linkEd2K);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error update link {linkEd2K.Title} - {ex.Message}");
                //return null;
            }
        }

        await _context.SaveChangesAsync();
        return linkEd2KList;
    }

    /// <summary>
    /// Marks a link as inactive in the database.
    /// </summary>
    /// <param name="linkEd2K">The link to delete.</param>
    /// <returns>The deleted link as a <see cref="DD_LinkEd2k"/> object.</returns>
    public async Task<DD_LinkEd2k> DeleteLinkEd2k(DD_LinkEd2k linkEd2K)
    {
        try
        {
            linkEd2K.IsActive = false;
            linkEd2K.LastUpdatedDate = DateTime.Now;
            var linkDeleted = _context.DDLinkEd2.Update(linkEd2K);
            await _context.SaveChangesAsync();

            return linkDeleted.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }

    
    // /// <summary>
    // /// Adds a new DD setting.
    // /// </summary>
    // /// <param name="newSetting">The new setting to add.</param>
    // /// <returns>The added setting as a <see cref="SettingDto"/> object.</returns>
    // public async Task<DDSettingDto> AddSetting(DDSettingDto newSetting)
    // {
    //     try
    //     {
    //         var setting = new Settings
    //         {
    //             CreatedDate = DateTime.Now,
    //             IsActive = true,
    //             LastUpdatedDate = DateTime.Now,
    //             UserName = newSetting.Dd_User,
    //             Password = await _utility.EncryptString(newSetting.Dd_Password)
    //         };
    //        
    //             await _context.DDSettings.AddAsync(setting);
    //
    //         await _context.SaveChangesAsync();
    //
    //         _logger.LogInformation($"Added new setting");
    //         return newSetting;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex.Message);
    //         return null!;
    //     }
    // }
}