using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;


namespace ElephantBackup
{
    public partial class BackupConfig
    {
        public static BackupConfig Create()
        {
            var doc = new BackupConfig()
            {
                BackupTarget = new BackupTarget()
                {
                    Path = string.Empty,
                    Verify = false
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
                    GlobalExclude = string.Empty
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
                NewLineHandling = NewLineHandling.Replace
            };
            using (var wtr = XmlWriter.Create(sb, settings))
            {
                var ser = new XmlSerializer(typeof(BackupConfig));
                ser.Serialize(wtr, this);
            }
            return sb.ToString();
        }


        public static string GetDefaultConfigFilePath()
        {
            return Path.Combine(
                Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"),
                "eb.config");
        }




        #region Loading

        private static DirectoryInfo[] defaultConfigDirs = new DirectoryInfo[]
        {
#if DEBUG
            new DirectoryInfo("../../"),
#endif
            new DirectoryInfo(Environment.CurrentDirectory),
            new DirectoryInfo(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")),
            new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
        };


        public static BackupConfig Load()
        {
            foreach(var path in defaultConfigDirs)
            {
                var config = Load(path);
                if (config != null)
                    return config;
            }
            return null;
        }


        private static string[] defaultConfigFilenames = new string[]
        {
            "eb.config",
            "elephantbackup.config"
        };

        public static BackupConfig Load(DirectoryInfo di)
        {
            foreach(var filename in defaultConfigFilenames)
            {
                var config = Load(new FileInfo(Path.Combine(di.FullName, filename)));
                if (config != null)
                    return config;
            }
            return null;
        }

        public static BackupConfig Load(FileInfo fi)
        {
            using (var stm = new FileStream(fi.FullName , FileMode.Open))
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
}
