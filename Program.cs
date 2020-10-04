using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffcut
{
    public class Program
    {
        static void Main(string[] args)
        {
            Options options = null;
            try
            {
                options = Options.Parse(args);
            }
            catch (OptionException e)
            {
                var message = e.OptionName == null ? e.Message : $"{e.Message}: \"{e.OptionName}\"";
                PrintUsage(message);
                Environment.Exit(1);
            }

            if (options.Help || string.IsNullOrEmpty(options.InputPath) || options.Times.Count == 0)
            {
                PrintUsage();
                Environment.Exit(2);
            }

            if (options.Verbose)
            {
                PrintMessage($"Input path: \"{options.InputPath}\"");
            }

            string ffmpegPath = GetffmpegPath();
            if (options.Verbose)
            {
                PrintMessage($"Using ffmpeg: \"{ffmpegPath}\"");
            }

            string inputName = Path.GetFileNameWithoutExtension(options.InputPath);
            string inputExtension = Path.GetExtension(options.InputPath);
            string outputPath = $"{inputName}-cut{inputExtension}";
            if (File.Exists(outputPath))
            {
                if (options.Force)
                {
                    File.Delete(outputPath);
                }
                else
                {
                    PrintError($"Output file '{outputPath}' already exists");
                    Environment.Exit(3);
                }
            }

            var parts = new List<string>();
            for (int i = 0; i < options.Times.Count; i++)
            {
                var timeRange = options.Times[i];
                var partName = $"{inputName}-ffcut-{i}{inputExtension}";
                var splitArgs = "";
                if (!string.IsNullOrEmpty(timeRange.From))
                {
                    splitArgs += $" -ss {timeRange.From}";
                }
                if (!string.IsNullOrEmpty(timeRange.To))
                {
                    splitArgs += $" -to {timeRange.To}";
                }
                splitArgs += $" -i \"{options.InputPath}\" -hide_banner -c copy -shortest -avoid_negative_ts 1 -map_chapters -1 \"{partName}\"";

                PrintMessage($"{ffmpegPath} {splitArgs}");
                var processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = splitArgs,
                    UseShellExecute = false,
                };

                using (var p = Process.Start(processInfo))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        Environment.Exit(255);
                    }
                }
                parts.Add(partName);
            }

            if (parts.Count == 1)
            {
                File.Move(parts[0], outputPath);
            }
            else
            {
                var partsPath = Path.Combine(Path.GetTempPath(), "ffcut-files.txt");
                var partsFileMode = FileMode.OpenOrCreate;
                if (File.Exists(partsPath))
                {
                    partsFileMode |= FileMode.Truncate;
                }
                using (var partsStream = new FileStream(partsPath, partsFileMode, FileAccess.Write))
                using (var partsWriter = new StreamWriter(partsStream))
                {
                    foreach (var part in parts)
                    {
                        partsWriter.WriteLine($"file '{part}'");
                    }
                }

                var combineArgs = $"-hide_banner -f concat -safe 0 -i \"{partsPath}\" -c copy \"{outputPath}\"";

                PrintMessage($"{ffmpegPath} {combineArgs}");
                var processInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = combineArgs,
                    UseShellExecute = false,
                };

                using (var p = Process.Start(processInfo))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        Environment.Exit(255);
                    }
                }

                foreach (var part in parts)
                {
                    File.Delete(part);
                }
                File.Delete(partsPath);
            }
        }

        private static void PrintError(string message)
        {
            Console.Error.WriteLine(message);
        }

        private static void PrintMessage(string message)
        {
            Console.WriteLine(message);
        }

        private static void PrintUsage(string message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                PrintError(message);
                PrintError("");
            }

            var exeName = AppDomain.CurrentDomain.FriendlyName;
            PrintError($"Usage: {exeName} [--help|-h] [--verbose|-v] [--force|-f] <input path> <hh:mm:ss>-<hh:mm:ss>|<mm:ss>-<mm:ss>...");
            PrintError("  --help, -h       Show usage");
            PrintError("  --verbose, -v    Show detailed progress messages");
            PrintError("  --force, -f      Overwrite existing files");
        }

        private static string GetffmpegPath()
        {
            var ffmpegName = "ffmpeg.exe";
            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ffmpegName);
            if (File.Exists(localPath))
            {
                return localPath;
            }

            return ffmpegName;
        }
    }
}
