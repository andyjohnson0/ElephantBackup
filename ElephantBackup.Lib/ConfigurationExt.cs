using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;

namespace uk.andyjohnson.ElephantBackup.Lib
{
    public static class ConfigurationFactory
    {
        /// <summary>
        /// Factory.
        /// </summary>
        /// <returns>Populated BackupConfig instance.</returns>
        public static Configuration Create()
        {
            var doc = new Configuration()
            {
                Target = new Target()
                {
                    Path = string.Empty
                },
                Source = new Source[]
                {
                    new Source()
                    {
                        Path = string.Empty,
                        ExcludeDirs = string.Empty,
                        ExcludeFileTypes = string.Empty
                    }
                },
                Options = new Options()
                {
                    GlobalExcludeFileTypes = string.Empty,
                    GlobalExcludeDirs = string.Empty,
                    Verify = true,
                    CreateLogFile = true
                }
            };
            return doc;
        }


        public static Configuration CreateExample()
        {
            var doc = ConfigurationFactory.Create();
            doc.Source[0].Path = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            doc.Options.GlobalExcludeFileTypes = ".obj;";
            doc.Options.GlobalExcludeDirs = "AppData;obj;bin";
            return doc;
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
            "../../..",  // project-lcal config file
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



        public static Configuration Load()
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


        public static Configuration Load(string path)
        {
            using (var stm = new FileStream(path, FileMode.Open))
            {
                var ser = new XmlSerializer(typeof(Configuration));
                var config = ser.Deserialize(stm) as Configuration;

                if (config.Target == null)
                    config.Target = new Target();
                config.Target.Path = BuildBackupTargetPath(config.Target.Path);

                return config;
            }
        }


        private static string BuildBackupTargetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && (drive.DriveType == DriveType.Removable))
                    {
                        return string.Format("{0}{1}_{2}",
                                             drive.Name, Environment.MachineName, DateTime.Now.ToString("yyyyMMddHHmmss"));
                    }
                }
                return null;
            }
            else
            {
                if (!path.EndsWith("\\"))
                    path = path + "\\";
                return Path.Combine(path, 
                                    string.Format("{0}_{1}", Environment.MachineName, DateTime.Now.ToString("yyyyMMddHHmmss")));
            }
        }

        #endregion Loading
    }
}
