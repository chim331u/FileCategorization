using FileCategorization_App.Data.Setting;

namespace FileCategorization_App.Components.Interface
{
    public interface IUtilityServices
    {
        Task CopyToClipboard(string text);
        string FormatAsEUR(object value);
        string FormatAsDate(object value);
        string FormatAsCurrency(double amountValue, string currency);
        string FileSizeFormatted(double len);

        IList<NetworkSetting> ReadNetworkSettingJson();
        bool WriteNetworkSettingJson(IList<NetworkSetting> settings);

        string ApiUrl { get; set; }
        string SetApiUrl();

        IList<GlobalSetting> ReadGlobalSettingJson();
        bool WriteGlobalSettingJson(IList<GlobalSetting> globalSettings);
        
        /// <summary>
        /// Whether to use v2 API endpoints
        /// </summary>
        bool UseV2Endpoints { get; set; }
        
        /// <summary>
        /// Whether to fallback to v1 endpoints if v2 fails
        /// </summary>
        bool FallbackToV1 { get; set; }
    }
}
