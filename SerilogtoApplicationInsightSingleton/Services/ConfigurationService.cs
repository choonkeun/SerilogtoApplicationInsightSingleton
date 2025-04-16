using Azure.Identity;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace SerilogtoApplicationInsightSingleton.Services;

public class ConfigurationService
{
    private readonly bool _isLocal;
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;

        // Azure WebApp의 환경 변수에서 isLocal 값을 확인
        var azureIsLocal = Environment.GetEnvironmentVariable("isLocal");
        
        if (!string.IsNullOrEmpty(azureIsLocal))
        {
            _isLocal = bool.Parse(azureIsLocal);
            Log.Information("Using isLocal setting from Azure Environment Variables: {IsLocal}", _isLocal);
        }
        else
        {
            _isLocal = _configuration.GetValue<bool>("AppSettings:isLocal");
            Log.Information("Using isLocal setting from appsettings.json: {IsLocal}", _isLocal);
        }
    }

    public string GetAppConfigurationConnectionString()
    {
        if (_isLocal)
        {
            // Local 환경에서는 appsettings.json에서 연결 문자열을 가져옵니다
            return _configuration["AppSettings:AzureAppConfigurationConnectionString"];
        }
        
        // Azure 환경에서는 환경 변수에서 연결 문자열을 가져옵니다
        return Environment.GetEnvironmentVariable("AzureAppConfigurationConnectionString");
    }

    public void ConfigureAzureAppConfiguration(IConfigurationBuilder builder)
    {
        var appConfigConnectionString = GetAppConfigurationConnectionString();
        
        if (string.IsNullOrEmpty(appConfigConnectionString))
        {
            throw new InvalidOperationException("Azure App Configuration connection string is not configured.");
        }

        builder.AddAzureAppConfiguration(options =>
        {
            options.Connect(appConfigConnectionString)
                .Select("appConfig:*", "Common")
                .Select("ConnectionString:*", "Common")
                .Select("ApplicationInsightsConnectionString", "Common")
                .Select("SerilogtoApplicationInsightSingleton:*", "SerilogtoApplicationInsightSingleton")
                .ConfigureKeyVault(kv =>
                {
                    kv.SetCredential(new DefaultAzureCredential());
                });
        });

        Log.Information("Application running in {Environment} environment", _isLocal ? "Local" : "Azure");
    }


}