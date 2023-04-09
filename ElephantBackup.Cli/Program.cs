using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

using uk.andyjohnson.ElephantBackup.Lib;


namespace uk.andyjohnson.ElephantBackup.Cli
{
    /// <summary>
    /// Main driver class.
    /// </summary>
    class Program
    {
        /// <summary>
        /// main entry point.
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>Completion staus. 0=ok, 1=error.</returns>
        public static int Main(string[] args)
        {
#if DEBUG
            // Set current dir to the project dir.
            Directory.SetCurrentDirectory("./../../..");
#endif

            return new Program().DoMain(args) ? 0 : 1;
        }


        /// <summary>
        /// Top-level driver function.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Success of requested operation.</returns>
        private bool DoMain(string[] args)
        { 
            var success = true;
            var cli = new CommandLineParser(args);

            if (cli.GetArg(new string[] { "help", "h", "?" } ))
            {
                DoHelp();
            }
            else if (cli.GetArg(new string[] { "createconfig", "cc" } ))
            {
                DoCreateConfig();
            }
            else
            {
                success = DoBackup();
            }

            // If exiting would cause the command window to immediately close then prompt the
            // user to press enter first so that they can see how the backup completed.
            // We do this by inspecting the console process list. If theres only one entry then
            // we prompt before exiting.
            if (GetConsoleProcessList(new uint[1], 1) == 1)
            {
                Console.WriteLine("[Press Enter to finish]");
                Console.ReadLine();
            }

            return success;
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] ProcessList, uint ProcessCount);


        /// <summary>
        /// Perform a backup.
        /// </summary>
        /// <returns>true if the backup succeeded</returns>
        private bool DoBackup()
        {
            var config = ConfigurationFactory.Load();
            if (config == null)
            {
                Console.WriteLine("Error: Config file not found.");
                return false;
            }
            if ((config?.Target == null) || (config.Target.Path == null))
            {
                Console.WriteLine("Error: No target specified and no default available");
                return false;
            }
            if ((config?.Source == null) || (config.Source.Length == 0))
            {
                Console.WriteLine("Error: No sources specified");
                return false;
            }

            Console.WriteLine($"Backing-up {config.Source.Length} source(s) to {config.Target.Path}");
            foreach(var source in config.Source)
            {
                Console.WriteLine($"    {source.Path}");
            }
            Console.WriteLine("Proceed? (Y/N)");
            if (Console.ReadLine().Trim().ToUpper() != "Y")
            {
                Console.WriteLine("Aborted");
                return false;
            }

            var bm = new BackupManager(config);
            bm.OnError += this.OnError;
            bm.OnInformation += this.OnInformation;

            var result = bm.DoBackup();  // actually do the backup.
            if (result.Success)
                Console.WriteLine("Backup succeeded after {0}", result.Timetaken.ToString(@"hh\:mm\:ss"));
            else
                Console.WriteLine("Backup failed after {0} - {1}",
                                    result.Timetaken.ToString(@"hh\:mm\:ss"),
                                    (result.Exception != null) ? result.Exception.Message : "Unknown reason");
            Console.WriteLine("Started at {0}", result.StartTime.ToString());
            Console.WriteLine("Finished at {0}", result.EndTime.ToString());
            Console.WriteLine("{0} bytes copied", result.BytesCopied.ToByteQuantity());
            Console.WriteLine("{0} files copied", result.FilesCopied);
            Console.WriteLine("{0} directories copied", result.DirectoriesCopied);
            Console.WriteLine("{0} files skipped", result.FilesSkipped);
            Console.WriteLine("{0} directories skipped", result.DirectoriesSkipped);
            if (result.LogFilePath != null)
                Console.WriteLine("Log file created at {0}", result.LogFilePath);

            return result.Success;
        }



        #region Event handlers

        private void OnInformation(object sender, BackupEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        private void OnError(object sender, BackupEventArgs args)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + args.Message);
            Console.ForegroundColor = c;
        }

        #endregion Event handlers



        private static void DoHelp()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            Console.WriteLine("Elephant Backup v{0} by Andy Johnson - https://andyjohnson.uk", version);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  eb [/? | /h | /help]");
            Console.WriteLine("    - Display this usage information");
            Console.WriteLine("  eb [/createconfig | /cc]");
            Console.WriteLine("    - Create a blank config file in the user's home directory");
            Console.WriteLine("  eb");
            Console.WriteLine("    - Perform a backup");
            Console.WriteLine();
            Console.WriteLine("For more info see https://github.com/andyjohnson0/ElephantBackup");
            Console.WriteLine();
        }


        /// <summary>
        /// Create a blank configuration file.
        /// </summary>
        private static void DoCreateConfig()
        {
            string configPath = ConfigurationFactory.GetConfigPath();
            if (File.Exists(configPath))
            {
                Console.WriteLine($"{configPath} already exists. Overwrite (y/n)?");
                if (Console.ReadLine().ToUpper() != "Y")
                    return;
            }

            using(var wtr = new StreamWriter(configPath, false, Encoding.UTF8))
            {
                var configStr = ConfigurationFactory.CreateExample().ToString();
                wtr.Write(configStr);
            }
            Console.WriteLine($"Config file written to {configPath}");
        }
    }
}
