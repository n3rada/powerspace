using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace PowerSpace
{

    [ComVisible(true)]
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            CommandParser parser = new CommandParser(args);
            parser.PrintParsedArguments();
            if (parser.IsValid)
            {
                PowerShellExecutor executor = new PowerShellExecutor();
                foreach (var moduleUrl in parser.ModuleUrls)
                {
                    executor.Execute(moduleUrl);
                }

                foreach (var command in parser.Commands)
                {
                    executor.Execute(command);
                }

                if (!string.IsNullOrEmpty(parser.EncodedCommand))
                {
                    executor.Execute(parser.EncodedCommand, true);
                }
            }
            else
            {
                Console.WriteLine("[x] Invalid command line arguments.");
            }
        }
    }

    class PowerShellExecutor
    {
        private readonly Runspace runspace;
        private readonly PowerShell powerShell;


        public PowerShellExecutor()
        {
            runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            powerShell = PowerShell.Create();
            powerShell.Runspace = runspace;
            SetupPowerShellEnvironment();
        }

        private void SetupPowerShellEnvironment()
        {
            var redefineWriteHost = @"
        function Write-Host {
            param([string]$Message)
            Write-Output $Message
        }";
            powerShell.AddScript(redefineWriteHost);
            powerShell.Invoke();
            powerShell.Commands.Clear();
        }

        public void Execute(string input, bool isEncoded = false)
        {

            string command = isEncoded ? DecodeBase64Command(input) : input;

            Console.WriteLine($"[+] Executing: {command}");

            if (Uri.IsWellFormedUriString(command, UriKind.Absolute))
            {
                command = FetchScriptFromUri(command);
            }

            if (string.IsNullOrEmpty(command))
            {
                Console.WriteLine("[x] No valid command or script to execute.");
                return;
            }


            powerShell.AddScript(command);
            powerShell.AddCommand("Out-String");

            try
            {
                var results = powerShell.Invoke();
                var output = new StringBuilder();
                AppendResults(output, results, powerShell);

                string trimmedOutput = output.ToString().Trim();

                if (trimmedOutput.Length > 0)
                {
                    Console.WriteLine("\n-------- Runspace Output --------\n");
                    Console.WriteLine(trimmedOutput);
                    Console.WriteLine("\n-------- Runspace Output --------\n");
                }
                else
                {
                    Console.WriteLine("[i] No output");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[x] Exception during execution: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                powerShell.Commands.Clear();
                ClearStreams();
            }
        }

        private string DecodeBase64Command(string encodedCommand)
        {
            try
            {
                byte[] commandBytes = Convert.FromBase64String(encodedCommand);
                return Encoding.Unicode.GetString(commandBytes);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"[x] Error decoding Base64 string: {ex.Message}");
                return null;
            }
        }

        private string FetchScriptFromUri(string moduleUri)
        {

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
                try
                {
                    return client.DownloadString(moduleUri);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[x] Error fetching script: {ex.Message}");
                    return null;
                }
            }
        }

        private void AppendResults(StringBuilder output, IEnumerable<PSObject> results, PowerShell powerShell)
        {
            AppendStreamResults(output, results);
            AppendStreamResults(output, powerShell.Streams.Error);
            AppendStreamResults(output, powerShell.Streams.Warning);
            AppendStreamResults(output, powerShell.Streams.Verbose);
            AppendStreamResults(output, powerShell.Streams.Debug);
        }
        private void AppendStreamResults<T>(StringBuilder output, IEnumerable<T> stream)
        {
            if (stream.Any())
            {
                foreach (var item in stream)
                {
                    output.AppendLine(item.ToString());
                }
            }
        }

        private void ClearStreams()
        {
            powerShell.Streams.Error.Clear();
            powerShell.Streams.Warning.Clear();
            powerShell.Streams.Verbose.Clear();
            powerShell.Streams.Debug.Clear();
        }

    }
    class CommandParser
    {
        public List<string> ModuleUrls { get; private set; } = new List<string>();
        public List<string> Commands { get; private set; } = new List<string>();
        public string EncodedCommand { get; private set; }
        public bool IsValid { get; private set; } = true;


        public CommandParser(string[] args)
        {
            string currentArg = string.Join(" ", args);

            Console.WriteLine($"[+] Received arguments: {currentArg}");

            ParseCommandOption(currentArg);
            ParseEncodedCommandOption(currentArg);
            ParseModuleOption(currentArg);

            IsValid = Commands.Count > 0 || ModuleUrls.Count > 0 || !string.IsNullOrEmpty(EncodedCommand);
        }

        private void ParseCommandOption(string currentArg)
        {
            var commandMatches = Regex.Matches(currentArg, @"\/c:(?<command>.+?)(?=\s+\/[cem]:|\s*$)");
            foreach (Match match in commandMatches)
            {
                if (match.Success)
                {
                    Commands.Add(match.Groups["command"].Value.Trim());
                }
            }
        }

        private void ParseEncodedCommandOption(string currentArg)
        {
            var encodedCommandMatch = Regex.Match(currentArg, @"\/e:(?<encodedCommand>.+?)(?:\s+\/[cm]:|$)");
            if (encodedCommandMatch.Success)
            {
                EncodedCommand = encodedCommandMatch.Groups["encodedCommand"].Value.Trim();
            }
        }

        private void ParseModuleOption(string currentArg)
        {
            var moduleMatches = Regex.Matches(currentArg, @"\/m:(?<module>[^\s]+?(?=(\s+\/[cem]:|\s*$)))");
            foreach (Match match in moduleMatches)
            {
                if (match.Success)
                {
                    ModuleUrls.Add(match.Groups["module"].Value.Trim());
                }
            }
        }


        public void PrintParsedArguments()
        {
            Console.WriteLine("\n[+] Parsed Arguments:");
            if (Commands.Count > 0)
            {
                foreach (var command in Commands)
                {
                    Console.WriteLine($"/c \t {command}");
                }
            }
            else
            {
                Console.WriteLine("/c \t null");
            }
            Console.WriteLine($"/e \t {EncodedCommand ?? "null"}");
            if (ModuleUrls.Count > 0)
            {
                foreach (var moduleUrl in ModuleUrls)
                {
                    Console.WriteLine($"/m \t {moduleUrl}");
                }
            }
            else
            {
                Console.WriteLine("/m \t null");
            }
            Console.WriteLine();
        }
    }
}
