using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;


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
                    DoBackup(source.Path, targetPath, config.Options.Verify, logFileWtr,
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


        private byte[] copyBuff = new byte[10240];

        private void DoBackup(
            string sourceDirPath,
            string targetDirPath,
            bool verify,
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
                string sourceHash, targetHash;
                CopyFile(sourceFilePath, targetFilePath, copyBuff, verify, out sourceHash, out targetHash);
                if (sourceHash != targetHash)
                    throw new IOException(string.Format("Backup verify failed for {0}", sourceFilePath));
                filesCopied += 1;
                bytesCopied += sourceFileLen;
                if (logFileWtr != null)
                {
                    logFileWtr.Write("{0}, {1} => {2}, {3} bytes",
                                     DateTime.Now, sourceFilePath, targetFilePath, sourceFileLen);
                    if (verify)
                        logFileWtr.Write(", {0}", sourceHash);
                    logFileWtr.WriteLine();
                }
            }
            foreach(var sourceSubdirPath in EnumerateDirectories(sourceDirPath,new string[0]))
            {
                var targetSubdirPath = Path.Combine(targetDirPath, sourceSubdirPath.Substring(sourceSubdirPath.LastIndexOf('\\') + 1));
                DoBackup(sourceSubdirPath, targetSubdirPath, verify, logFileWtr, ref bytesCopied, ref filesCopied, ref directoriesCopied);
                directoriesCopied += 1;
            }
        }




        private static void CopyFile(
            string sourceFilePath,
            string targetFilePath,
            byte[] copyBuff,
            bool verify,
            out string sourceHashStr,
            out string targetHashStr)
        {
            MD5 sourceHash = verify ? MD5.Create() : null;
            using (var sourceStm = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            {
                using (var targetStm = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                {
                    while (true)
                    {
                        var bytesRead = sourceStm.Read(copyBuff, 0, copyBuff.Length);
                        targetStm.Write(copyBuff, 0, bytesRead);
                        if (sourceHash != null)
                        {
                            sourceHash.TransformBlock(copyBuff, 0, bytesRead, copyBuff, 0);
                        }
                        if (bytesRead < copyBuff.Length)
                        {
                            if (sourceHash != null)
                            {
                                sourceHash.TransformFinalBlock(copyBuff, 0, copyBuff.Length);
                            }
                            break;
                        }
                    }
                }
            }
            sourceHashStr = (sourceHash.Hash != null) ? HashToString(sourceHash.Hash) : null;

            if (verify)
            {
                var targetHash = MD5.Create();
                using (var targetStm = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read))
                {
                    while (true)
                    {
                        var bytesRead = targetStm.Read(copyBuff, 0, copyBuff.Length);
                        targetHash.TransformBlock(copyBuff, 0, bytesRead, copyBuff, 0);
                        if (bytesRead < copyBuff.Length)
                        {
                            targetHash.TransformFinalBlock(copyBuff, 0, copyBuff.Length);
                            break;
                        }
                    }
                }
                targetHashStr = HashToString(targetHash.Hash);
            }
            else
            {
                targetHashStr = null;
            }
        }


        private static string HashToString(byte[] hash)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
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
