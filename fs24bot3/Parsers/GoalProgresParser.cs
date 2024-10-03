using Qmmands;
using System;
using System.Threading.Tasks;

namespace fs24bot3.Parsers;

public class GoalProgress 
{
    public uint Progress;
    public uint Total = 0;

    public GoalProgress(string input)
    {
        string[] progressAndTotal = input.Split("/");

        if (progressAndTotal.Length > 1)
        {
            Progress = uint.TryParse(progressAndTotal[0], out Progress) ? Progress : 0;
            Total = uint.TryParse(progressAndTotal[1], out Total) ? Total : 1; 
        }
        else
        {
            if (!uint.TryParse(input, out Progress))
            {
                throw new FormatException($"`{input}` is not a valid string for constructing GoalProgress class");
            }
        }
    }

    public override string ToString()
    {
        return $"{Progress}/{Total}";
    }
}

public class GoalProgressParser : TypeParser<GoalProgress>
{
    public override ValueTask<TypeParserResult<GoalProgress>> ParseAsync(Parameter parameter, string value,
        CommandContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TypeParserResult<GoalProgress>.Failed("Value cannot be null or whitespace.");

        return TypeParserResult<GoalProgress>.Successful(new GoalProgress(value));
    }
}
