using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Angular.Hubs
{
    public class WeatherForecastHub : Hub<IAngularFrontendInstance>
    {

        private static readonly string[] Summaries = {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<IEnumerable<WeatherForecast>> Get()
        {
            var rng = new Random();
            Clients.Client(Context.ConnectionId).NewWeatherForecast(new WeatherForecast
            {
                Date = DateTime.Now,
                TemperatureC = 20,
                Summary = "Foo"
            });
            return Task.FromResult<IEnumerable<WeatherForecast>>(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
                .ToArray());
        }
    }

    public interface IAngularFrontendInstance
    {
        Task NewWeatherForecast(WeatherForecast weatherForecast);
    }
}
