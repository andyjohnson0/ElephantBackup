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

            long bytesCopied = 0L, filesCopied = 0L, directoriesCopied = 0L;
            try
            {
                foreach (var source in config.BackupSource)
                {
                    DoBackup(source.Path, config.BackupTarget.Path, ref bytesCopied, ref filesCopied, ref directoriesCopied);
                }
                result.Success = true;
            }
            catch(Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.BytesCopied = bytesCopied;
                result.FilesCopied = filesCopied;
                result.DirectoriesCopied = directoriesCopied;
            }

            return result;
        }


        private void DoBackup(
            string sourceDirPath,
            string targetDirPath,
            ref long bytesCopied,
            ref long filesCopied,
            ref long directoriesCopied)
        {
            Directory.CreateDirectory(targetDirPath);

            foreach(var sourceFilePath in EnumerateFiles(sourceDirPath, new string[0]))
            {
                var targetFilePath = Path.Combine(targetDirPath, sourceFilePath.Substring(sourceFilePath.LastIndexOf('\\') + 1));
                if (callbacks != null)
                    callbacks.FileBackupMessage(sourceFilePath, targetFilePath);
                File.Copy(sourceFilePath, targetFilePath);
                filesCopied += 1;
                bytesCopied += new FileInfo(sourceFilePath).Length;
            }
            foreach(var sourceSubdirPath in EnumerateDirectories(sourceDirPath,new string[0]))
            {
                var targetSubdirPath = Path.Combine(targetDirPath, sourceSubdirPath.Substring(sourceSubdirPath.LastIndexOf('\\') + 1));
                DoBackup(sourceSubdirPath, targetSubdirPath, ref bytesCopied, ref filesCopied, ref directoriesCopied);
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

    }
}
