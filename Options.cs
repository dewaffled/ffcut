using Mono.Options;
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
        public List<TimeRange> Times { get; set; } = new List<TimeRange>();

        public static Options Parse(string[] args)
        {
            var options = new Options();
            var flags = new OptionSet
            {
                { "h|?|help", v => options.Help = true },
                { "v|verbose", v => options.Verbose = true },
                { "f|force", v => options.Force = true },
            };

            var unprocessed = flags.Parse(args);
            foreach (var arg in unprocessed)
            {
                ProcessArg(arg, options);
            }

            return options;
        }

        private static void ProcessArg(string arg, Options options)
        {
            if (arg.StartsWith("-") && arg.Length > 1 && !char.IsDigit(arg[1]))
            {
                throw new OptionException("Unknown command line option", arg);
            }

            if (options.InputPath == null)
            {
                options.InputPath = arg;
                return;
            }

            if (arg.Count(c => { return c == '-'; }) != 1)
            {
                throw new OptionException("Invalid time interval", arg);
            }

            var range = new TimeRange();
            var timeParts = arg.Split('-');
            if (timeParts.Length > 0 && !string.IsNullOrEmpty(timeParts[0]))
            {
                range.From = timeParts[0];
                if (!IsTimeSpan(range.From))
                {
                    throw new OptionException($"Invalid start time '{range.From}' of interval", arg);
                }
            }
            if (timeParts.Length > 1 && !string.IsNullOrEmpty(timeParts[1]))
            {
                range.To = timeParts[1];
                if (!IsTimeSpan(range.To))
                {
                    throw new OptionException("Invalid end time '{range.To}' of interval", arg);
                }
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
