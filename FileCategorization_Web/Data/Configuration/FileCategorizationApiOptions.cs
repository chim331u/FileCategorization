namespace FileCategorization_Web.Data.Configuration;

public class FileCategorizationApiOptions
{
    public const string SectionName = "FileCategorizationApi";
    
    public string BaseUrl { get; set; } = "";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
}

public class RetryPolicyOptions
{
    public int MaxRetries { get; set; } = 3;
    public int BackoffMultiplier { get; set; } = 2;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
}