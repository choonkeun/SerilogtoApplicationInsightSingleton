using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace SerilogtoApplicationInsightSingleton.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        //use IConfiguration as dependency injection: appsettings.json, environment variables, or other configuration sources
        private readonly IConfiguration _configuration;
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public WeatherForecastController(
            ILogger<WeatherForecastController> logger,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            bool isLocal = bool.Parse(_configuration["IsLocal"]);
            _logger.LogInformation("Environment: {Environment}: WeatherForecast", isLocal ? "Local" : "Azure");
            _logger.LogInformation("physicalPath: {physicalPath}: WeatherForecast", Directory.GetCurrentDirectory());
            _logger.LogInformation("contentRootPath: {contentRootPath}: WeatherForecast", _webHostEnvironment.ContentRootPath);
            _logger.LogInformation("webRootPath: {webRootPath}: WeatherForecast", _webHostEnvironment.WebRootPath);

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
