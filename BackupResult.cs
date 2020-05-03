using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup
{
    /// <summary>
    /// Represents the result of a backup operation.
    /// </summary>
    public class BackupResult
    {
        /// <summary>
        /// Overall success/failure.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// start time of backup.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of backup.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Time taken for backup.
        /// </summary>
        public TimeSpan Timetaken
        {
            get { return EndTime - StartTime; }
        }

        /// <summary>
        /// Number of byts copied.
        /// </summary>
        public long BytesCopied { get; set; }

        /// <summary>
        /// Number of files copied.
        /// </summary>
        public long FilesCopied { get; set; }

        /// <summary>
        /// Number of directories copied.
        /// </summary>
        public long DirectoriesCopied { get; set; }

        /// <summary>
        /// Number of files skipped.
        /// </summary>
        public long FilesSkipped { get; set; }

        /// <summary>
        /// Number of directories skipped.
        /// </summary>
        public long DirectoriesSkipped { get; set; }

        /// <summary>
        /// path to log file.
        /// </summary>
        public string LogFilePath { get; set; }

        /// <summary>
        /// Exceptin causing filure, or null on success.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
