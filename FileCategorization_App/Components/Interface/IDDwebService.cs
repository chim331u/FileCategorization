using FileCategorization_App.Data.DTOs;

namespace FileCategorization_App.Components.Interface;

public interface IDDwebService
{
    Task<List<ThreadsDto>> GetActiveThreads();
    Task<List<Ed2kLinkDto>> GetEd2kLinks(int threadId);
    Task<string> UseLink(int linkId);

    Task<bool> RenewThread(int threadId);
    Task<bool> CheckUrl(string urlToCheck);
}