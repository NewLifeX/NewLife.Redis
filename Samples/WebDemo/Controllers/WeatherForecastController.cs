using Microsoft.AspNetCore.Mvc;
using NewLife;
using NewLife.Caching;

namespace WebDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ICacheProvider cacheProvider, ILogger<WeatherForecastController> logger)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        var cache = _cacheProvider.Cache;
        var v = cache.Get<String>("test");
        if (v.ToInt() <= 0) cache.Remove("test");
        cache.SetExpire("test", TimeSpan.FromSeconds(60));

        var v2 = _cacheProvider.Cache.Increment("test", 1);

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = (Int32)v2,
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
