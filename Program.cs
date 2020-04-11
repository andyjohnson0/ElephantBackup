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
            return new Program().DoBackup(args);
        }





        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] ProcessList, uint ProcessCount);


        private int DoBackup(string[] args)
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

            if (GetConsoleProcessList(new uint[1], 1) == 1)
            {
                Console.WriteLine("[Press Enter to finish]");
                Console.ReadLine();
            }

            return result.Success ? 0 : 1;
        }



        #region IBackupCallbacks

        void IBackupCallbacks.FileBackupMessage(string sourceFilePath, string targetFilePath)
        {
            Console.WriteLine("{0} => {1}", sourceFilePath, targetFilePath);
        }

        #endregion IBackupCallbacks
    }
}
