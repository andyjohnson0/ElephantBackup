using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup.Lib
{
    public class BackupEventArgs : EventArgs
    {
        public BackupEventArgs(
            string message,
            params object[] args)
        {
            this.Message = string.Format(message, args);
        }

        public string Message { get; set; }

        public Exception Exception { get; set; }
    }
}
