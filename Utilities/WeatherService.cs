using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Drawing;

namespace MissionPlanner.Utilities
{
    public class WeatherData
    {
        public CurrentWeather current { get; set; }
        public Location location { get; set; }
    }

    public class Location
    {
        public string name { get; set; }
        public string region { get; set; }
        public string country { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class CurrentWeather
    {
        public double temp_c { get; set; }
        public double temp_f { get; set; }
        public Condition condition { get; set; }
        public double wind_mph { get; set; }
        public double wind_kph { get; set; }
        public int wind_degree { get; set; }
        public string wind_dir { get; set; }
        public double pressure_mb { get; set; }
        public double pressure_in { get; set; }
        public double precip_mm { get; set; }
        public double precip_in { get; set; }
        public int humidity { get; set; }
        public int cloud { get; set; }
        public double feelslike_c { get; set; }
        public double feelslike_f { get; set; }
        public double vis_km { get; set; }
        public double vis_miles { get; set; }
        public double uv { get; set; }
        public double gust_mph { get; set; }
        public double gust_kph { get; set; }
    }

    public class Condition
    {
        public string text { get; set; }
        public string icon { get; set; }
        public int code { get; set; }
    }

    public class WeatherService
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string ApiKey = "6d63264e221c40aca85101427262401";

        public static async Task<WeatherData> GetCurrentAsync(double lat, double lon)
        {
            return await GetCurrentAsync($"{lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        public static async Task<WeatherData> GetCurrentAsync(string query)
        {
            try
            {
                var url = $"https://api.weatherapi.com/v1/current.json?key={ApiKey}&q={Uri.EscapeDataString(query)}&aqi=no";
                var json = await _http.GetStringAsync(url);
                return JsonConvert.DeserializeObject<WeatherData>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to fetch weather data: " + ex.Message);
                return null;
            }
        }
    }
}
