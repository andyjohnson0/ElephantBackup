using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uk.andyjohnson.ElephantBackup.Cli
{
    /// <summary>
    /// callback interface for a program hostingBackupmanager class.
    /// </summary>
    public interface IBackupCallbacks
    {
        /// <summary>
        /// Handle an information message.
        /// </summary>
        /// <param name="message">Information message.</param>
        /// <param name="args">Argumentd</param>
        void InfoMessage(string message, params string[] args);

        /// <summary>
        /// Handle an error message.
        /// </summary>
        /// <param name="message">Information message.</param>
        /// <param name="args">Argumentd</param>
        void ErrorMessage(string message, params string[] args);
    }
}
