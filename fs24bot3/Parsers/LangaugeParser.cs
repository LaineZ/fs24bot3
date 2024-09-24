using Qmmands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Parsers
{

    public class Language
    {
        public string From;
        public string To;

        public Language(string input)
        {
            string[] langs = input.Split("-");

            if (langs.Length > 1)
            {
                From = langs[0];
                To = langs[1]; 
            } else
            {
                throw new FormatException($"`{input}` is not a valid string for constructing Language class");
            }
        }

        public override string ToString()
        {
            return $"{From}-{To}";
        }
    }
    public class LanugageParser : TypeParser<Language>
    {
        public override ValueTask<TypeParserResult<Language>> ParseAsync(Parameter parameter, string value, CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(value))
                return TypeParserResult<Language>.Failed("Value cannot be null or whitespace.");

            if (value.All(char.IsLetter))
                return TypeParserResult<Language>.Failed("Both parts of value must consist of only letters.");

            return TypeParserResult<Language>.Successful(new Language(value));
        }
    }
}
