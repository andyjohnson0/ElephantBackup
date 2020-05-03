using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;


namespace uk.andyjohnson.ElephantBackup
{
    /// <summary>
    /// Manages the backup process.
    /// </summary>
    public class BackupManager
    {
        /// <summary>
        /// Constructor. Initialise a BackupManager object.
        /// </summary>
        /// <param name="config">Configuation.</param>
        /// <param name="callbacks">Reference to an object implementing IBackupCallbacks.</param>
        public BackupManager(
            BackupConfig config,
            IBackupCallbacks callbacks)
        {
            this.config = config;
            this.callbacks = callbacks;
        }

        private BackupConfig config;
        private IBackupCallbacks callbacks;


        /// <summary>
        /// Performa backup using the supplied configuration.
        /// </summary>
        /// <returns>BackupResult object describing the outcome.</returns>
        public BackupResult DoBackup()
        {
            var result = new BackupResult();
            result.StartTime = DateTime.Now;

            StreamWriter logFileWtr = null;
            if ((config.Options != null) && config.Options.CreateLogFile)
            {
                // Create log file.
                Directory.CreateDirectory(config.BackupTarget.Path);
                result.LogFilePath = Path.Combine(config.BackupTarget.Path, "backup.log");
                logFileWtr = new StreamWriter(result.LogFilePath, false);
                logFileWtr.WriteLine("Starting at {0}", result.StartTime);
            }

            try
            {
                foreach (var source in config.BackupSource)
                {
                    // Create a top-level directory for each backup root in the configuration.
                    // We need to make sure these are unique.
                    var targetPath = BuildTargetPath(new DirectoryInfo(source.Path).Name,
                                                     config.BackupTarget.Path);
                    DoBackup(source.Path, targetPath, config.Options.Verify, logFileWtr, result);
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
                if (logFileWtr != null)
                {
                    logFileWtr.WriteLine("Finished at {0}", result.EndTime);
                    logFileWtr.Close();
                }
            }

            return result;
        }


        private byte[] copyBuff = new byte[10240];

        /// <summary>
        /// backup a directory.
        /// </summary>
        /// <param name="sourceDirPath">Source directory.</param>
        /// <param name="targetDirPath">Target directory.</param>
        /// <param name="verify">Verify each file copy?</param>
        /// <param name="logFileWtr">Log file writer. Can be null if no logging.</param>
        /// <param name="progress">Backup result to be updated.</param>
        private void DoBackup(
            string sourceDirPath,
            string targetDirPath,
            bool verify,
            StreamWriter logFileWtr,
            BackupResult progress)
        {
            Directory.CreateDirectory(targetDirPath);

            // Backup files.
            IEnumerable<string> filesEnum = null;
            try
            {
                filesEnum = EnumerateFiles(sourceDirPath, new string[0]);
            }
            catch(System.UnauthorizedAccessException ex)
            {
                progress.DirectoriesSkipped += 1;
                var msg = string.Format("Failed to enumerate {0}. Skipped.", sourceDirPath);
                if (logFileWtr != null)
                {
                    logFileWtr.WriteLine(msg);
                }
                callbacks.ErrorMessage(msg, ex);
                return;
            }

            foreach(var sourceFilePath in filesEnum)
            {
                var targetFilePath = Path.Combine(targetDirPath, sourceFilePath.Substring(sourceFilePath.LastIndexOf('\\') + 1));
                var sourceFileLen = new FileInfo(sourceFilePath).Length;
                if (callbacks != null)
                    callbacks.FileBackupMessage(sourceFilePath, targetFilePath);
                string sourceHash, targetHash;
                try
                {
                    CopyFile(sourceFilePath, targetFilePath, copyBuff, verify, out sourceHash, out targetHash);
                }
                catch(UnauthorizedAccessException ex)
                {
                    progress.FilesSkipped += 1;
                    var msg = string.Format("Failed to copy {0}. Skipped.", sourceFilePath);
                    if (logFileWtr != null)
                    {
                        logFileWtr.WriteLine(msg);
                    }
                    callbacks.ErrorMessage(msg, ex);
                    continue;
                }
                if (sourceHash != targetHash)
                    throw new IOException(string.Format("Backup verify failed for {0}", sourceFilePath));
                progress.FilesCopied += 1;
                progress.BytesCopied += sourceFileLen;
                if (logFileWtr != null)
                {
                    logFileWtr.Write("{0}, {1} => {2}, {3} bytes",
                                     DateTime.Now, sourceFilePath, targetFilePath, sourceFileLen);
                    if (verify)
                        logFileWtr.Write(", {0}", sourceHash);
                    logFileWtr.WriteLine();
                }
            }

            // Recurse subdirectories.
            foreach(var sourceSubdirPath in EnumerateDirectories(sourceDirPath,new string[0]))
            {
                var targetSubdirPath = Path.Combine(targetDirPath, sourceSubdirPath.Substring(sourceSubdirPath.LastIndexOf('\\') + 1));
                DoBackup(sourceSubdirPath, targetSubdirPath, verify, logFileWtr, progress);
                progress.DirectoriesCopied += 1;
            }
        }



        /// <summary>
        /// copy a file with optional verify
        /// </summary>
        /// <param name="sourceFilePath">source file path.</param>
        /// <param name="targetFilePath">Target file path.</param>
        /// <param name="copyBuff">Copy buffer.</param>
        /// <param name="verify">Verify copy?</param>
        /// <param name="sourceHashStr">(out) Source file MD5 hash.</param>
        /// <param name="targetHashStr">(out) Target file MD5 hash.</param>
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

            // if we're verifying the copy then re-hash the target file.
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


        /// <summary>
        /// Utility function to convert an MD5 hash to a hex string.
        /// </summary>
        /// <param name="hash">MD5 hash</param>
        /// <returns>String representation</returns>
        private static string HashToString(byte[] hash)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }



        /// <summary>
        /// Utility function to enumerate files in a directory with the option to exclude specified extensions.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="excludeExtensions">Extensions to exclude (e.g. ".obj")</param>
        /// <returns>Enumerator</returns>
        private static IEnumerable<string> EnumerateFiles(
            string path,
            string[] excludeExtensions)
        {
            return Directory.GetFiles(path)
                            .Where(file => !excludeExtensions.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Utility function to enumerate subdirectories in a directory with the option to exclude specified names.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="excludeExtensions">Directories to exclude (e.g. "bin")</param>
        /// <returns>Enumerator</returns>
        private static IEnumerable<string> EnumerateDirectories(
            string path,
            string[] exclude)
        {
            return Directory.GetDirectories(path)
                            .Where(dir => !exclude.Any(x => dir.IndexOf(x, StringComparison.OrdinalIgnoreCase) != -1));
        }


        /// <summary>
        /// Create a unique name for a directory within a parent.
        /// </summary>
        /// <param name="sourceDirName">directory name.</param>
        /// <param name="targetParentDirPath">Base name.</param>
        /// <returns>Unique name.</returns>
        private static string BuildTargetPath(
            string sourceDirName, 
            string targetParentDirPath)
        {
            for(var i = 0; i < 100000; i++)
            {
                var path = Path.Combine(targetParentDirPath, "backup");
                path = Path.Combine(path, sourceDirName);
                path = Path.Combine(path, (i == 0) ? string.Empty : i.ToString());
                if (!Directory.Exists(path))
                    return path;
            }

            throw new Exception("Too many source root directories with same name: " + sourceDirName);
        }

    }
}
