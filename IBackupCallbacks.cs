using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup
{
    public interface IBackupCallbacks
    {
        void FileBackupMessage(string sourceFilePath, string targetFilePath);
    }
}
