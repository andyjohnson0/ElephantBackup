using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup
{
    public class BackupException : Exception
    {
        public BackupException(string message) : base(message)
        {
        }

        public BackupException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BackupManager BackupManager { get; set; }
    }
}
