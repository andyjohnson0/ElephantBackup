using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace uk.andyjohnson.ElephantBackup
{
    /// <summary>
    /// Main driver class.
    /// </summary>
    class Program : IBackupCallbacks
    {
        /// <summary>
        /// main entry point.
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>Completion staus. 0=ok, 1=error.</returns>
        static int Main(string[] args)
        {
            return new Program().DoMain(args) ? 0 : 1;
        }




        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] ProcessList, uint ProcessCount);


        /// <summary>
        /// Top-level driver function.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Success of requested operation.</returns>
        private bool DoMain(string[] args)
        { 
            var success = true;
            var cli = new CommandLineParser(args);

            if (cli.GetArg(new string[] { "help", "?" } ))
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


        /// <summary>
        /// Perform a backup.
        /// </summary>
        /// <returns>true if the backup succeeded</returns>
        private bool DoBackup()
        {
            var config = BackupConfig.Load();
            if (config == null)
            {
                Console.WriteLine("Error: Config file not found.");
                return false;
            }
            if ((config?.BackupTarget == null) || (config.BackupTarget.Path == null))
            {
                Console.WriteLine("Error: No target specified");
                return false;
            }
            if ((config?.BackupSource == null) || (config.BackupSource.Length == 0))
            {
                Console.WriteLine("Error: No sources specified");
                return false;
            }

            var bm = new BackupManager(config, this);
            var result = bm.DoBackup();  // actually do the backup.
            if (result.Success)
                Console.WriteLine("Backup succeeded after {0}", result.Timetaken);
            else
                Console.WriteLine("Backup failed after {0} - {1}",
                                    result.Timetaken,
                                    (result.Exception != null) ? result.Exception.Message : "Unknown reason");
            Console.WriteLine("Started at {0}", result.StartTime.ToString());
            Console.WriteLine("Finished at {0}", result.EndTime.ToString());
            Console.WriteLine("{0} bytes copied", result.BytesCopied);
            Console.WriteLine("{0} files copied", result.FilesCopied);
            Console.WriteLine("{0} directories copied", result.DirectoriesCopied);
            Console.WriteLine("{0} files skipped", result.FilesSkipped);
            Console.WriteLine("{0} directories skipped", result.DirectoriesSkipped);
            if (result.LogFilePath != null)
                Console.WriteLine("Log file created at {0}", result.LogFilePath);

            return result.Success;
        }



        #region IBackupCallbacks

        void IBackupCallbacks.InfoMessage(string message, params string[] args)
        {
            Console.WriteLine(message, args);
        }

        void IBackupCallbacks.ErrorMessage(string message, params string[] args)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + message, args);
            Console.ForegroundColor = c;
        }

        #endregion IBackupCallbacks



        private void DoHelp()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            Console.WriteLine("Elephant Backup v{0} by Andy Johnson - https://andyjohnson.uk", version);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  eb [/? | /help]");
            Console.WriteLine("    - Display this usage information");
            Console.WriteLine("  eb /createconfig");
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
        private void DoCreateConfig()
        {
            string configPath = BackupConfig.GetConfigPath();
            if (File.Exists(configPath))
            {
                Console.WriteLine("{0} already exists. Overwrite (y/n)?", configPath);
                if (Console.ReadLine() != "y")
                    return;
            }

            using(var wtr = new StreamWriter(configPath, false, Encoding.UTF8))
            {
                var configStr = BackupConfig.CreateExample().ToString();
                wtr.Write(configStr);
            }
            Console.WriteLine("Config file written to {0}", configPath);
        }
    }
}
