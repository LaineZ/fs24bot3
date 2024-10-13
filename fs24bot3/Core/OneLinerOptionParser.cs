using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace fs24bot3.Core;
class OneLinerOptionParser
{
    public Dictionary<string, string> Options { get; }
    public List<string> AllowedOptions { get; set; }
    public string RetainedInput { get; }

    private readonly Regex SearchTermRegex = new Regex(
    @"^(
            \s*
            (?<term>
                ((?<prefix>[a-zA-Z][a-zA-Z0-9-_]*):)?
                (?<termString>
                    (?<quotedTerm>
                        (?<quote>['""])
                        ((\\\k<quote>)|((?!\k<quote>).))*
                        \k<quote>?
                    )
                    |(?<simpleTerm>[^\s]+)
                )
            )
            \s*
        )*$",
    RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture
    );


    public OneLinerOptionParser(string input)
    {
        RetainedInput = input;
        Options = new Dictionary<string, string>();
        AllowedOptions = new List<string>();

        Match match = SearchTermRegex.Match(input);
        foreach (Capture term in match.Groups["term"].Captures.Cast<Capture>())
        {
            Capture prefix = null;
            foreach (Capture prefixMatch in match.Groups["prefix"].Captures.Cast<Capture>())
                if (prefixMatch.Index >= term.Index && prefixMatch.Index <= term.Index + term.Length)
                {
                    prefix = prefixMatch;
                    break;
                }

            Capture termString = null;
            foreach (Capture termStringMatch in match.Groups["termString"].Captures.Cast<Capture>())
                if (termStringMatch.Index >= term.Index && termStringMatch.Index <= term.Index + term.Length)
                {
                    termString = termStringMatch;
                    break;
                }

            if (prefix != null)
            {
                RetainedInput = RetainedInput.Replace($"{prefix.Value}:{termString.Value}", "");
                Log.Verbose("option: {0} value: {1}", prefix.Value, termString.Value);

                if (AllowedOptions.Any() && !AllowedOptions.Contains(prefix.Value.ToLower()))
                {
                    continue;
                }

                Options[prefix.Value] = termString.Value;
            }

            RetainedInput = RetainedInput.TrimStart();
        }


        Log.Verbose("Retained: {0}", RetainedInput);
    }
}
