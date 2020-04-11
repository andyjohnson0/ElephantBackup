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
    }
}
