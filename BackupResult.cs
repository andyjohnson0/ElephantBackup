using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup
{
    public class BackupResult
    {
        public bool Success { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Timetaken
        {
            get { return EndTime - StartTime; }
        }

        public long BytesCopied { get; set; }

        public long FilesCopied { get; set; }

        public long DirectoriesCopied { get; set; }

        public string LogFilePath { get; set; }

        public Exception Exception { get; set; }
    }
}
