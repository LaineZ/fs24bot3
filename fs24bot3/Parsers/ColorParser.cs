using System;
using System.Drawing;
using System.Threading.Tasks;
using Qmmands;

namespace fs24bot3.Parsers;

public class ColorParser : TypeParser<Color>
{
    public override ValueTask<TypeParserResult<Color>> ParseAsync(Parameter parameter, string input,
        CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
            return TypeParserResult<Color>.Failed("Value cannot be null or whitespace.");
        
        if (input.StartsWith("#"))
        {
            return TypeParserResult<Color>.Successful(ColorTranslator.FromHtml(input));
        }

        if (input.StartsWith("0x"))
        {
            return TypeParserResult<Color>.Successful(ColorTranslator.FromHtml(input.Replace("0x", "#")));
        }

        var colorComponents = input.Split(",");

        if (colorComponents.Length == 3)
        {
            var r = 0;
            var g = 0;
            var b = 0;

            r = int.TryParse(colorComponents[0], out r) ? r : 0;
            g = int.TryParse(colorComponents[1], out g) ? g : 0;
            b = int.TryParse(colorComponents[2], out b) ? b : 0;

            return TypeParserResult<Color>.Successful(ColorTranslator.FromHtml($"rgb({r}, {g}, {b})"));
        }

        return TypeParserResult<Color>.Failed("Unknown color format");
    }
}