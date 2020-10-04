using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ffcut
{
    public class Options
    {
        public class TimeRange
        {
            public string From { get; set; }
            public string To { get; set; }
        }

        public bool Help { get; set; }
        public bool Verbose { get; set; }
        public bool Force { get; set; }
        public string InputPath { get; set; }
        public List<TimeRange> Times { get; set; }

        public static Options Parse(string[] args)
        {
            var options = new Options();
            foreach (var s in args)
            {
                ParseArg(s, options);
            }
            return options;
        }

        private static void ParseArg(string arg, Options options)
        {
            if (arg.StartsWith("--"))
            {
                switch (arg)
                {
                    case "--help":
                        options.Help = true;
                        break;
                    case "--verbose":
                        options.Verbose = true;
                        break;
                    case "--force":
                        options.Force = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown command line option: \"{arg}\"");
                }
                return;
            }

            if (arg.StartsWith("-") && arg.Length > 1 && !char.IsDigit(arg[1]))
            {
                foreach (var c in arg.Skip(1))
                {
                    switch (c)
                    {
                        case 'h':
                            options.Help = true;
                            break;
                        case 'v':
                            options.Verbose = true;
                            break;
                        case 'f':
                            options.Force = true;
                            break;
                        default:
                            throw new ArgumentException($"Unknown command line option: \"-{c}\"");
                    }
                }
                return;
            }
            
            if (options.InputPath == null)
            {
                options.InputPath = arg;
                return;
            }

            if (arg.Count(c => { return c == '-'; }) != 1)
            {
                throw new ArgumentException($"Invalid time interval: \"{arg}\"");
            }

            var range = new TimeRange();
            var timeParts = arg.Split('-');
            if (timeParts.Length > 0 && !string.IsNullOrEmpty(timeParts[0]))
            {
                range.From = timeParts[0];
                if (!IsTimeSpan(range.From))
                {
                    throw new ArgumentException($"Invalid start time '{range.From}' of interval: \"{arg}\"");
                }
            }
            if (timeParts.Length > 1 && !string.IsNullOrEmpty(timeParts[1]))
            {
                range.To = timeParts[1];
                if (!IsTimeSpan(range.To))
                {
                    throw new ArgumentException($"Invalid end time '{range.To}' of interval: \"{arg}\"");
                }
            }

            if (options.Times == null)
            {
                options.Times = new List<TimeRange>();
            }
            options.Times.Add(range);
        }

        private static bool IsTimeSpan(string s)
        {
            if (new Regex(@"^\d?\d:\d?\d:\d?\d$").IsMatch(s))
            {
                return true;
            }

            if (new Regex(@"^\d?\d:\d?\d$").IsMatch(s))
            {
                return true;
            }

            if (new Regex(@"^\d?\d$").IsMatch(s))
            {
                return true;
            }

            return false;
        }
    }
}
