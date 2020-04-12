using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ElephantBackup
{
    public class BackupManager
    {
        public BackupManager(
            BackupConfig config,
            IBackupCallbacks callbacks)
        {
            this.config = config;
            this.callbacks = callbacks;
        }

        private BackupConfig config;
        private IBackupCallbacks callbacks;


        public BackupResult DoBackup()
        {
            var result = new BackupResult();
            result.StartTime = DateTime.Now;

            StreamWriter logFileWtr = null;
            if ((config.Options != null) && config.Options.CreateLogFile)
            {
                Directory.CreateDirectory(config.BackupTarget.Path);
                result.LogFilePath = Path.Combine(config.BackupTarget.Path, "backup.log");
                logFileWtr = new StreamWriter(result.LogFilePath, false);
                logFileWtr.WriteLine("Starting at {0}", result.StartTime);
            }

            long bytesCopied = 0L, filesCopied = 0L, directoriesCopied = 0L;
            try
            {
                foreach (var source in config.BackupSource)
                {
                    var targetPath = BuildTargetPath(new DirectoryInfo(source.Path).Name,
                                                     config.BackupTarget.Path);
                    DoBackup(source.Path, targetPath, logFileWtr,
                             ref bytesCopied, ref filesCopied, ref directoriesCopied);
                }
                result.Success = true;
            }
            catch(Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
                if (logFileWtr != null)
                {
                    logFileWtr.WriteLine("{0} : Error: {1}", DateTime.Now, ex.Message);
                }
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.BytesCopied = bytesCopied;
                result.FilesCopied = filesCopied;
                result.DirectoriesCopied = directoriesCopied;
                if (logFileWtr != null)
                {
                    logFileWtr.WriteLine("Finished at {0}", result.EndTime);
                    logFileWtr.Close();
                }
            }

            return result;
        }


        private void DoBackup(
            string sourceDirPath,
            string targetDirPath,
            StreamWriter logFileWtr,
            ref long bytesCopied,
            ref long filesCopied,
            ref long directoriesCopied)
        {
            Directory.CreateDirectory(targetDirPath);

            foreach(var sourceFilePath in EnumerateFiles(sourceDirPath, new string[0]))
            {
                var targetFilePath = Path.Combine(targetDirPath, sourceFilePath.Substring(sourceFilePath.LastIndexOf('\\') + 1));
                var sourceFileLen = new FileInfo(sourceFilePath).Length;
                if (callbacks != null)
                    callbacks.FileBackupMessage(sourceFilePath, targetFilePath);
                File.Copy(sourceFilePath, targetFilePath);
                filesCopied += 1;
                bytesCopied += sourceFileLen;
                if (logFileWtr != null)
                    logFileWtr.WriteLine("{0} : {1} => {2} : {3} bytes",
                                         DateTime.Now, sourceFilePath, targetFilePath, sourceFileLen);
            }
            foreach(var sourceSubdirPath in EnumerateDirectories(sourceDirPath,new string[0]))
            {
                var targetSubdirPath = Path.Combine(targetDirPath, sourceSubdirPath.Substring(sourceSubdirPath.LastIndexOf('\\') + 1));
                DoBackup(sourceSubdirPath, targetSubdirPath, logFileWtr, ref bytesCopied, ref filesCopied, ref directoriesCopied);
                directoriesCopied += 1;
            }
        }






        private static IEnumerable<string> EnumerateFiles(
            string path,
            string[] excludeExtensions)
        {
            return Directory.GetFiles(path)
                            .Where(file => !excludeExtensions.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)));
        }


        private static IEnumerable<string> EnumerateDirectories(
            string path,
            string[] exclude)
        {
            return Directory.GetDirectories(path)
                            .Where(dir => !exclude.Any(x => dir.IndexOf(x, StringComparison.OrdinalIgnoreCase) != -1));
        }


        private static string BuildTargetPath(
            string sourceDirName, 
            string targetParentDirPath)
        {
            for(var i = 0; i < 100000; i++)
            {
                var path = Path.Combine(targetParentDirPath,
                                        "backup",
                                        sourceDirName,
                                        (i == 0) ? string.Empty : i.ToString());
                if (!Directory.Exists(path))
                    return path;
            }

            throw new Exception("Too many source root directories with same name: " + sourceDirName);
        }

    }
}
