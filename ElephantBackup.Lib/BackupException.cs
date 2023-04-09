using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup.Lib
{
    /// <summary>
    /// Wraps exceptions occurring during a backup.
    /// </summary>
    public class BackupException : Exception
    {
        public BackupException(string message) : base(message)
        {
        }

        public BackupException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Faulting BackupManager instance.
        /// </summary>
        public BackupManager BackupManager { get; set; }
    }
}
