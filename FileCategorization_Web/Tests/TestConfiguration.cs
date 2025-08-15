using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = false)]

namespace FileCategorization_Web.Tests;

/// <summary>
/// Global test configuration and setup
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Default timeout for async operations in tests
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Test data constants
    /// </summary>
    public static class TestData
    {
        public const string SampleFileName = "TestFile.txt";
        public const string SampleCategory = "TestCategory";
        public const int DefaultSearchParameter = 3;
        
        public static readonly List<string> SampleCategories = new()
        {
            "Documents",
            "Images", 
            "Videos",
            "Music"
        };
    }
    
    /// <summary>
    /// Cache test constants
    /// </summary>
    public static class CacheTestData
    {
        public const string FilesKeyPrefix = "files_";
        public const string CategoriesKey = "categories";
        public const string ConfigurationsKey = "configurations";
        
        public static readonly TimeSpan ShortExpiration = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan LongExpiration = TimeSpan.FromHours(1);
    }
}