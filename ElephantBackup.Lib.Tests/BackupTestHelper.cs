using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uk.andyjohnson.ElephantBackup.Lib;

namespace ElephantBackup.Lib.Tests
{
    internal static class BackupTestHelper
    {
        public static bool DoBackup(
            int numDirLevels = 1,
            int numFilesPerDir = 10,
            int numSubdirsPerDir = 1,
            int fileLen = 10240)
        {
            var di = CreateTempDir("ElephantBackup_");
            var sourceDi = di.CreateSubdirectory("Source");
            CreateSourceFiles(sourceDi, numDirLevels, numFilesPerDir, numSubdirsPerDir, fileLen);
            var targetDi = di.CreateSubdirectory("Target");
            var config = new Configuration()
            {
                Source = new Source[1]
                {
                    new Source()
                    {
                        Path = sourceDi.FullName
                    }
                },
                Target = new Target()
                {
                    Path = targetDi.FullName
                },
                Options = new Options()
                {
                    GlobalExcludeFileTypes = ""
                }
            };
            var bm = new BackupManager(config);
            var result = bm.DoBackup();
            if (!result.Success)
                return false;
            if (!ValidateBackup(sourceDi, new DirectoryInfo(Path.Combine(result.RootDirectory.FullName, sourceDi.Name))))
                return false;
            di.Delete(true);
            return true;
        }



        private static void CreateSourceFiles(
            DirectoryInfo di,
            int numDirLevels,
            int numFilesPerDir,
            int numSubdirsPerDir,
            int fileLen)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var rnd = new Random();

            for(int nFile = 0; nFile <= numFilesPerDir; nFile++)
            {
                var s = new string(' ', fileLen);
                s = new string(s.Select(c => chars[rnd.Next(chars.Length)]).ToArray());
                using (var wtr = new StreamWriter(Path.Combine(di.FullName, $"file{nFile}.dat")))
                {
                    wtr.Write(s);
                }
            }

            if (numDirLevels > 1)
            {
                for (int iSubdir = 0; iSubdir < numSubdirsPerDir; iSubdir++)
                {
                    CreateSourceFiles(di.CreateSubdirectory($"directory{iSubdir + 1}"), numDirLevels - 1, numFilesPerDir, numSubdirsPerDir, fileLen);
                }
            }
        }


        private static bool ValidateBackup(
            DirectoryInfo sourceDi,
            DirectoryInfo targetDi)
        {
            if (!sourceDi.Exists)
                return false;
            if (!targetDi.Exists)
                return false;

            foreach(var sourceFi in sourceDi.GetFiles())
            {
                var sourceBytes = new byte[sourceFi.Length];
                using (var sourceStm = new FileStream(sourceFi.FullName, FileMode.Open))
                {
                    if (sourceStm.Read(sourceBytes, 0, sourceBytes.Length) != sourceBytes.Length)
                        return false;
                }

                var targetFi = new FileInfo(Path.Combine(targetDi.FullName, sourceFi.Name));
                if (!targetFi.Exists)
                    return false;
                var targetBytes = new byte[targetFi.Length];
                using (var targetStm = new FileStream(targetFi.FullName, FileMode.Open))
                {
                    if (targetStm.Read(targetBytes, 0, targetBytes.Length) != targetBytes.Length)
                        return false;
                }

                if (sourceBytes.Length != targetBytes.Length)
                    return false;
                for (var i = 0; i < sourceBytes.Length; i++)
                {
                    if (sourceBytes[i] != targetBytes[i])
                        return false;
                }
            }

            foreach(var sourceSubDi in sourceDi.GetDirectories())
            {
                var targetSubDi = new DirectoryInfo(Path.Combine(targetDi.FullName, sourceSubDi.Name));
                if (!targetSubDi.Exists)
                    return false;
                if (!ValidateBackup(sourceSubDi, targetSubDi))
                    return false;
            }

            return true;
        }


        /// <summary>
        /// Create a temporary directory with an optional name suffix
        /// </summary>
        /// <param name="suffix">P[tional name suffix.</param>
        /// <returns>DirectoryInfo object referenving the temporary directory.</returns>
        private static DirectoryInfo CreateTempDir(string suffix = "")
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), suffix + Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);
        }    
    }
}
;