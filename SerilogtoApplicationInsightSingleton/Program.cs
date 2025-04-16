#define USEAPPSETTING
#define SKIPFILE
#define PRINTCONFIG

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using SerilogtoApplicationInsightSingleton.Services;
using System.Configuration;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container for dependency injection
builder.Services.AddSingleton<ConfigurationService>();

// Configure Azure App Configuration
var configService = new ConfigurationService(builder.Configuration);
configService.ConfigureAzureAppConfiguration(builder.Configuration);

// Configure Serilog
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);

// Azure App Configuration에서 Application Insights 연결 문자열 가져오기
var applicationInsightsConnectionString = builder.Configuration["ApplicationInsightsConnectionString"];

if (string.IsNullOrEmpty(applicationInsightsConnectionString))
{
    throw new InvalidOperationException("Application Insights connection string is not configured in Azure App Configuration.");
}

Console.WriteLine("---Program Start---");
Console.WriteLine($"applicationInsightsConnectionString: {applicationInsightsConnectionString}");
Console.WriteLine("");

//yahoo > application Insight > ApplicationInsights-ALL

Log.Logger = new LoggerConfiguration()

#if USEAPPSETTING
    //"UseSerilog()"가 logging Control을 가져올때 AppSettings.Logging를 참조하도록 한다
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Information()
#else
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Information)
#endif

    //** For all logging: Set "ApplicationName" on Application Insight custome property **
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ApplicationName", "SerilogtoApplicationInsightSingleton")     

    .WriteTo.Console()

#if no_SKIPFILE
    .WriteTo.File(
        "Logs/appLog-.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext}|{Message}{NewLine:1}{Exception:1}",
        rollingInterval: RollingInterval.Day, 
        restrictedToMinimumLevel: LogEventLevel.Information
    )
#endif

    .WriteTo.ApplicationInsights(
        connectionString: applicationInsightsConnectionString,
        telemetryConverter: new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter()
    )
    .CreateLogger();

builder.Host.UseSerilog();      //이문장으로 Microsoft.Extensions.Logging은 무시되고 Serilog로만 동작하게 된다.

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "A sample API to demonstrate Swagger",
        //Contact = new OpenApiContact
        //{
        //    Name = "Your Name",
        //    Email = "your.email@example.com",
        //}
    });
});

var app = builder.Build();

// Swagger UI는 항상 사용 가능하도록 설정
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseSerilogRequestLogging();         //logging HTTP

// Log application start with environment info
bool isLocal = Environment.GetEnvironmentVariable("isLocal") is string keyLocal
    ? bool.Parse(keyLocal)
    : builder.Configuration.GetValue<bool>("AppSettings:isLocal");

// Add values to configuration to be referenced using dependency injection
builder.Configuration["IsLocal"] = isLocal.ToString();


#if no_PRINTCONFIG
var appConfiguration = builder.Configuration as IConfigurationRoot;
var azureAppConfigurationProvider = appConfiguration.Providers.OrderBy(p => p.GetType().Name)
    .FirstOrDefault(x => x.GetType().Name == "AzureAppConfigurationProvider");

if (appConfiguration != null)
{
    var allEnvironmentVariables = new Dictionary<string, string>();
    var allAppConfiguration = appConfiguration.AsEnumerable().Select(x => x.Key).OrderBy(p => p.GetType().Name);

    // Iterate over keys and retrieve values: Total 112
    foreach (var key in allAppConfiguration)
    {
        var value = appConfiguration[key];
        if (!string.IsNullOrEmpty(value))
        {
            allEnvironmentVariables[key] = value;
        }
    }

    // Print all environment variables
    int index = 0;
    Console.WriteLine($"Print all environment variables");
    foreach (var kvp in allEnvironmentVariables)
    {
        Console.WriteLine($"{index++} --> {kvp.Key}: {kvp.Value}");
    }
}
else
{
    Console.WriteLine("Environment Variables Provider Not Found!");
}
#endif


Log.Information("Application Started in {Environment} environment", isLocal ? "Local" : "Azure");
app.Run();
