namespace fs24bot3.Models
{
    public class WeatherGeneric
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double WindSpeed { get; set; }
        public int Humidity { get; set; }
        public string CityName { get; set; }
        public WeatherConditions Condition { get; set; }
        public WindDirections WindDirection { get; set; }
    }
    
}