using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;


namespace uk.andyjohnson.ElephantBackup.Lib
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
        public BackupManager(
            Configuration config)
        {
            this.config = config;
        }

        private Configuration config;
        private StreamWriter logFileWtr;

        public EventHandler<BackupEventArgs> OnError;
        public EventHandler<BackupEventArgs> OnInformation;


        /// <summary>
        /// Performa backup using the supplied configuration.
        /// </summary>
        /// <returns>BackupResult object describing the outcome.</returns>
        public BackupResult DoBackup()
        {
            this.OnError += this.OnEvent_Error;
            this.OnInformation += this.OnEvent_Information;

            var result = new BackupResult();
            result.StartTime = DateTime.Now;

            try
            {
                Directory.CreateDirectory(config.Target.Path);
            }
            catch(Exception ex)
            {
                OnError?.Invoke(this,
                                new BackupEventArgs($"Failed to create target directory at {config.Target.Path}"));
                result.Success = false;
                result.Exception = ex;
                result.EndTime = DateTime.Now;
                return result;
            }

            if ((config.Options != null) && config.Options.CreateLogFile)
            {
                // Create log file.
                result.LogFilePath = Path.Combine(config.Target.Path, "backup.log");
                logFileWtr = new StreamWriter(result.LogFilePath, false);
                OnInformation?.Invoke(this, new BackupEventArgs($"Starting at {result.StartTime}"));
            }

            var rootPath = Path.Combine(config.Target.Path, DateTime.Now.ToString("yyyyMMdd_hhmmss"));

            try
            {
                foreach (var source in config.Source)
                {
                    var excludeFileTypes = Combine(source.GetExcludeFileTypes(), config.Options.GetExcludeFileTypes());
                    var excludeDirs = Combine(source.GetExcludeDirs(), config.Options.GetExcludeDirs());

                    // Create a top-level directory for each backup root in the configuration.
                    // We need to make sure these are unique.
                    var targetPath = BuildTargetPath(new DirectoryInfo(source.Path).Name, rootPath);
                    DoBackup(source.Path, targetPath, config.Options.Verify, excludeFileTypes, excludeDirs, result);
                }
                result.Success = true;
            }
            catch(Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
                if (logFileWtr != null)
                {
                    logFileWtr.WriteLine($"{DateTime.Now} : Error: {ex.Message}");
                }
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.RootDirectory = new DirectoryInfo(rootPath);
                if (logFileWtr != null)
                {
                    logFileWtr.WriteLine($"Finished at {result.EndTime}");
                    logFileWtr.Close();
                    logFileWtr = null;
                }
                this.OnError -= this.OnEvent_Error;
                this.OnInformation -= this.OnEvent_Information;
            }

            return result;
        }


        private byte[] copyBuff = new byte[10 * 1024 * 1024];

        /// <summary>
        /// backup a directory.
        /// </summary>
        /// <param name="sourceDirPath">Source directory.</param>
        /// <param name="targetDirPath">Target directory.</param>
        /// <param name="verify">Verify each file copy?</param>
        /// <param name="excludeFileTypes">File types to exclude</param>
        /// <param name="excludeDirs">Directory names to exclude</param>
        /// <param name="progress">Backup result to be updated.</param>
        private void DoBackup(
            string sourceDirPath,
            string targetDirPath,
            bool verify,
            string[] excludeFileTypes,
            string[] excludeDirs,
            BackupResult progress)
        {
            Directory.CreateDirectory(targetDirPath);

            // Backup files.
            IEnumerable<string> filesEnum = null;
            try
            {
                filesEnum = EnumerateFiles(sourceDirPath, excludeFileTypes);
            }
            catch(System.UnauthorizedAccessException ex)
            {
                progress.DirectoriesSkipped += 1;
                OnError?.Invoke(this, new BackupEventArgs($"{ex.Message}: Directory skipped."));

                return;
            }

            foreach(var sourceFilePath in filesEnum)
            {
                var targetFilePath = Path.Combine(targetDirPath, sourceFilePath.Substring(sourceFilePath.LastIndexOf('\\') + 1));
                var sourceFileLen = new FileInfo(sourceFilePath).Length;
                OnInformation?.Invoke(this, new BackupEventArgs($"{sourceFilePath} => {targetFilePath}"));
                string sourceHash, targetHash;
                try
                {
                    CopyFile(sourceFilePath, targetFilePath, copyBuff, verify, out sourceHash, out targetHash);
                }
                catch(Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        progress.FilesSkipped += 1;
                        OnError?.Invoke(this, new BackupEventArgs($"{ex.Message}: File skipped"));
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
                if (sourceHash != targetHash)
                {
                    throw new IOException(string.Format($"Backup verify failed for {sourceFilePath}"));
                }
                progress.FilesCopied += 1;
                progress.BytesCopied += sourceFileLen;
                OnInformation?.Invoke(this,
                                      new BackupEventArgs("{0}, {1} => {2}, {3} bytes {4}",
                                                          DateTime.Now, sourceFilePath, targetFilePath, sourceFileLen,
                                                          verify ? sourceHash : ""));
            }

            // Recurse subdirectories.
            foreach (var sourceSubdirPath in EnumerateDirectories(sourceDirPath, excludeDirs))
            {
                var targetSubdirPath = Path.Combine(targetDirPath, sourceSubdirPath.Substring(sourceSubdirPath.LastIndexOf('\\') + 1));
                DoBackup(sourceSubdirPath, targetSubdirPath, verify, excludeFileTypes, excludeDirs, progress);
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
            sourceHashStr = (sourceHash?.Hash != null) ? HashToString(sourceHash.Hash) : null;

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
        /// <param name="sourceDirName">Directory name.</param>
        /// <param name="targetParentDirPath">Base path.</param>
        /// <returns>Unique name.</returns>
        private static string BuildTargetPath(
            string sourceDirName, 
            string targetParentDirPath)
        {
            for(var i = 0; i < 100000; i++)
            {
                var path = Path.Combine(targetParentDirPath, sourceDirName, (i == 0) ? string.Empty : i.ToString());
                if (!Directory.Exists(path))
                    return path;
            }

            throw new InvalidOperationException($"Too many source root directories with same name: {sourceDirName}");
        }


        public static string[] Combine(
            string[] arr1,
            string[] arr2)
        {
            int len1 = (arr1 != null) ? arr1.Length : 0;
            int len2 = (arr2 != null) ? arr2.Length : 0;

            var arr = new string[len1 + len2];
            if (len1 > 0)
                Array.Copy(arr1, 0, arr, 0, len1);
            if (len2 > 0)
                Array.Copy(arr2, 0, arr, len1, len2);

            return arr;
        }


        #region Event handlers

        private void OnEvent_Information(object sender, BackupEventArgs args)
        {
            if (logFileWtr != null)
            {
                logFileWtr.WriteLine(args.Message);
            }
        }

        private void OnEvent_Error(object sender, BackupEventArgs args)
        {
            if (logFileWtr != null)
            {
                logFileWtr.WriteLine($"Error: {args.Message}");
            }
        }

        #endregion Event handlers
    }
}
