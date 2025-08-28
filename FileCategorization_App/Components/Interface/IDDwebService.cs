using FC_APP.Data;
using FC_APP.Data.DTOs;

namespace FC_APP.Components.Interface;

public interface IDDwebService
{
    Task<List<ThreadsDto>> GetActiveThreads();
    Task<List<Ed2kLinkDto>> GetEd2kLinks(int threadId);
    Task<string> UseLink(int linkId);

    Task<bool> RenewThread(int threadId);
    Task<bool> CheckUrl(string urlToCheck);
}