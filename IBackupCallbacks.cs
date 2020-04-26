using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup
{
    /// <summary>
    /// callback interface for a program hostingBackupmanager class.
    /// </summary>
    public interface IBackupCallbacks
    {
        /// <summary>
        /// Display a message.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="targetFilePath">Target file path.</param>
        void FileBackupMessage(string sourceFilePath, string targetFilePath);
    }
}
