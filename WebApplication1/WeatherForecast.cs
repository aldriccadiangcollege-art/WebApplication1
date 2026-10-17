

using System.ComponentModel.DataAnnotations;

namespace WebApplication1
{
    public class WeatherForecast
    {
        [Required(ErrorMessage= "Whatever message you want to display")]
        public DateOnly Date { get; set; }

        [Range(0, 100, ErrorMessage ="Allowed value is from 0 to 100")]
        public int TemperatureC { get; set; }

        [RegularExpression(@"\d{2}")]
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        [StartsWith("Admin", ErrorMessage = "Whatever")]
        public string? Summary { get; set; }
    }
}
