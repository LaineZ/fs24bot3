using System.ComponentModel;

namespace fs24bot3.Models;

public enum WeatherConditions
{
    [Description("Ясно")]
    Clear,
    [Description("Переменная облачность")]
    PartlyCloudy,
    [Description("Облачно")]
    Cloudy,
    [Description("Пасмурно")]
    Overcast,
    [Description("Морось")]
    Drizzle,
    [Description("Мелкий дождь")]
    LightRain,
    [Description("Дождь")]
    Rain,
    [Description("Умеренный дождь")]
    ModerateRain,
    [Description("Сильный дождь")]
    HeavyRain,
    [Description("Длительный сильный дождь")]
    ContinuousHeavyRain,
    [Description("Ливень")]
    Showers,
    [Description("Мокрый снег")]
    WetSnow,
    [Description("Рыхлый снег")]
    WightSnow,
    [Description("Снег")]
    Snow,
    [Description("Снегопад")]
    SnowShowers,
    [Description("Град")]
    Hail,
    [Description("Гроза")]
    Thunderstorm,
    [Description("Гроза с дождем")]
    ThunderstormWithRain,
    [Description("Гроза с градом")]
    ThunderstormWithHail,
}

public enum WindDirections {
    [Description("⬉")]
    Nw,
    [Description("⬆")]
    N,
    [Description("⬈")]
    Ne,
    [Description("⮕")]
    E,
    [Description("⬊")]
    Se,
    [Description("⬇")]
    S,
    [Description("⬋")]
    Sw,
    [Description("⬅")]
    W,
    [Description("*")]
    C,
}