using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;



namespace ElephantBackup
{
    class Program : IBackupCallbacks
    {
        static int Main(string[] args)
        {
            return new Program().DoMain(args) ? 0 : 1;
        }




        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] ProcessList, uint ProcessCount);


        private bool DoMain(string[] args)
        { 
            var success = true;
            var cli = new CommandLineParser(args);

            if (cli.GetArg(new string[] { "?", "help" }))
            {
                DoHelp();
            }
            else if (cli.GetArg("createconfig"))
            {
                DoCreateConfig();
            }
            else
            {
                success = DoBackup();
            }


            if (GetConsoleProcessList(new uint[1], 1) == 1)
            {
                Console.WriteLine("[Press Enter to finish]");
                Console.ReadLine();
            }

            return success;
        }


        private bool DoBackup()
        {
            var config = BackupConfig.Load();
            var bm = new BackupManager(config, this);
            var result = bm.DoBackup();
            if (result.Success)
                Console.WriteLine("Backup succeeded after {0)", result.Timetaken);
            else
                Console.WriteLine("Backup failed after {0} - {1}",
                                    result.Timetaken,
                                    (result.Exception != null) ? result.Exception.Message : "Unknown reason");
            Console.WriteLine("Started at {0}", result.StartTime.ToString());
            Console.WriteLine("Finished at {0}", result.EndTime.ToString());
            Console.WriteLine("{0} bytes copied", result.BytesCopied);
            Console.WriteLine("{0} files copied", result.FilesCopied);
            Console.WriteLine("{0} directories copied", result.DirectoriesCopied);

            return result.Success;
        }



        #region IBackupCallbacks

        void IBackupCallbacks.FileBackupMessage(string sourceFilePath, string targetFilePath)
        {
            Console.WriteLine("{0} => {1}", sourceFilePath, targetFilePath);
        }

        #endregion IBackupCallbacks



        private void DoHelp()
        {
            Console.WriteLine("ElephantBackup by Andy Johnson - andy@andyjohnson.uk");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  eb [/? | /help]");
            Console.WriteLine("    - Display this usage information");
            Console.WriteLine("  eb /createconfig");
            Console.WriteLine("    - Create a blank config file in the user's home directory");
            Console.WriteLine("  eb");
            Console.WriteLine("    - Perform a backup");
            Console.WriteLine();
        }


        private void DoCreateConfig()
        {
            string configPath = BackupConfig.GetDefaultConfigFilePath();
            if (File.Exists(configPath))
            {
                Console.WriteLine("{0} already exists. Overwrite (y/n)?", configPath);
                if (Console.ReadLine() != "y")
                    return;
            }

            using(var wtr = new StreamWriter(configPath, false))
            {
                var configStr = BackupConfig.CreateExample().ToString();
                wtr.Write(configStr);
            }
            Console.WriteLine("Config file written to {0}", configPath);
        }


    }
}
