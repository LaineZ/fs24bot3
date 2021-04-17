using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qmmands;

namespace fs24bot3.Core
{
    class OneLinerOptionParser
    {
        public List<(string, string)> Options { get; }
        public string RetainedInput { get; }

        public OneLinerOptionParser(string input)
        {
            RetainedInput = input;
            Options = new List<(string, string)>();

            string[] queryOptions = input.Split(" ");

            for (int i = 0; i < queryOptions.Length; i++)
            {
                string[] options = queryOptions[i].Split(":");
                if (options.Length > 1)
                {
                    if (options[1].StartsWith('"'))
                    {
                        foreach (string value in queryOptions.Skip(i + 1))
                        {
                            options[1] += " " + value;
                            if (value.EndsWith('"')) { break; }
                        }
                    }
                    Options.Add((options[0], options[1]));
                    RetainedInput = RetainedInput.Replace($"{options[0]}:{options[1]}", "");
                }
            }
        }
    }
}
