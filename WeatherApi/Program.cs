var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.MapGet("/weather/{zipcode}", GetWeather);

app.Run();

static async Task<IResult> GetWeather(string zipcode)
{
    using var client = new HttpClient
    {
        BaseAddress = new Uri("http://api.openweathermap.org")
    };

    var apiKey = ""; //https://home.openweathermap.org/api_keys
    var location = await client.GetFromJsonAsync<OpenWeatherLocation>($"/geo/1.0/zip?zip={zipcode},US&appid={apiKey}");
    if (location is null)
    {
        return TypedResults.NotFound();
    }
    var weather = await client.GetFromJsonAsync<OpenWeatherResponse>($"/data/2.5/forecast?lat={location.Lat}&lon={location.Lon}&units=imperial&appid={apiKey}");
    if (weather is null)
    {
        return TypedResults.NotFound();
    }

    var forecasts = weather.List
        .Select(f => (DateTimeOffset.FromUnixTimeSeconds(f.Dt), f.Main.Temp_min, f.Main.Temp_max))
        .GroupBy(f => f.Item1.Date)
        .Select(g => new WeatherForecast(DateOnly.FromDateTime(g.Key), g.Min(f => f.Temp_min), g.Max(f => f.Temp_min)))
        .ToArray();

    var response = new WeatherResponse(location, forecasts);

    return TypedResults.Ok(response);
}

record WeatherResponse(OpenWeatherLocation Location, WeatherForecast[] Forecast);
record WeatherForecast(DateOnly Date, decimal TempMin, decimal TempMax);

record OpenWeatherLocation(string Zip, string Name, double Lat, double Lon, string Country);
record OpenWeatherResponse(OpenWeatherForecast[] List);
record OpenWeatherForecast(int Dt, string Dt_txt, OpenWeatherTemps Main);
record OpenWeatherTemps(decimal Temp, decimal Temp_min, decimal Temp_max);
