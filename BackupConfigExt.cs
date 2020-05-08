using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;


namespace uk.andyjohnson.ElephantBackup
{
    /// <summary>
    /// Extension methods for the auto-generated Backupconfig class.
    /// </summary>
    public partial class BackupConfig
    {
        /// <summary>
        /// Factory.
        /// </summary>
        /// <returns>Populated BackupConfig instance.</returns>
        public static BackupConfig Create()
        {
            var doc = new BackupConfig()
            {
                BackupTarget = new BackupTarget()
                {
                    Path = string.Empty
                },
                BackupSource = new BackupSource[]
                {
                    new ElephantBackup.BackupSource()
                    {
                        Path = string.Empty
                    }
                },
                Options = new Options()
                {
                    GlobalExcludeFileTypes = string.Empty,
                    Verify = false,
                    CreateLogFile = true
                }
            };
            return doc;
        }


        public static BackupConfig CreateExample()
        {
            var doc = BackupConfig.Create();
            doc.BackupTarget.Path = "Path to root of back-up. " +
                                    "Omit or leave blank to back-up to the first removable device.";
            doc.BackupSource[0].Path = "Folder to back-up. Add more as needed.";
            return doc;
        }


        public override string ToString()
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = Encoding.UTF8
            };
            using (var wtr = XmlWriter.Create(sb, settings))
            {
                var ser = new XmlSerializer(typeof(BackupConfig));
                ser.Serialize(wtr, this);
            }
            return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
        }


        public static string GetConfigPath()
        {
            foreach(var dir in defaultConfigDirs)
            {
                foreach(var name in defaultConfigFilenames)
                {
                    var path = Path.Combine(dir.ToString(), name);
                    if (File.Exists(path))
                        return path;
                }
            }

            return Path.Combine(defaultConfigDirs[0].ToString(), defaultConfigFilenames[0]);
        }




        #region Loading

        /// <summary>
        /// Folders (in order) to search for config file.
        /// </summary>
        private static string[] defaultConfigDirs = new string[]
        {
#if DEBUG
            "../../",
#endif
            Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.CurrentDirectory
        };


        /// <summary>
        /// Config file names.
        /// </summary>
        private static string[] defaultConfigFilenames = new string[]
        {
            "eb.config",
            "elephantbackup.config"
        };



        public static BackupConfig Load()
        {
            foreach(var dir in defaultConfigDirs)
            {
                foreach (var filename in defaultConfigFilenames)
                {
                    var path = Path.Combine(dir, filename);
                    if (File.Exists(path))
                    {
                        var config = Load(path);
                        if (config != null)
                            return config;
                    }
                }
            }
            return null;
        }


        public static BackupConfig Load(string path)
        {
            using (var stm = new FileStream(path, FileMode.Open))
            {
                var root = new XmlRootAttribute();
                root.ElementName = "BackupConfig";
                var ser = new XmlSerializer(typeof(BackupConfig), root);
                var config = ser.Deserialize(stm) as BackupConfig;

                if (config.BackupTarget == null)
                    config.BackupTarget = new BackupTarget();
                if (string.IsNullOrEmpty(config.BackupTarget.Path))
                {
                    config.BackupTarget.Path = BuildBackupTargetPath();
                }

                return config;
            }
        }


        private static string BuildBackupTargetPath()
        {
            foreach(var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && (drive.DriveType == DriveType.Removable))
                {
                    return string.Format("{0}{1}_{2}",
                                         drive.Name, Environment.MachineName, DateTime.Now.ToString("yyyyMMddhhmmss"));
                }
            }

            return null;
        }

        #endregion Loading
    }


    public partial class BackupSource
    {
        public string[] GetExcludeFileTypes()
        {
            return this.ExcludeFileTypes?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetExcludeDirs()
        {
            return this.ExcludeDirs?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }


    public partial class Options
    {
        public string[] GetExcludeFileTypes()
        {
            return this.GlobalExcludeFileTypes?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetExcludeDirs()
        {
            return this.GlobalExcludeDirs?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
