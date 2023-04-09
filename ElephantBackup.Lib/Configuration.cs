using System;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace uk.andyjohnson.ElephantBackup.Lib
{    
    /// <remarks/>
    [Serializable]
    public class Configuration
    {
        public Target Target { get; set; }
        
        public Source[] Source { get; set; }

        public Options Options { get; set; }


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
                var ser = new XmlSerializer(typeof(Configuration));
                ser.Serialize(wtr, this);
            }
            return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
        }
    }

    
    [Serializable]
    public class Target
    {
        public string Path { get; set; }
    }
    
    /// <remarks/>
    [Serializable]
    public class Source
    {
        public string Path { get; set; }

        public string ExcludeFileTypes { get; set; }

        public string ExcludeDirs { get; set; }

        public string[] GetExcludeFileTypes()
        {
            return this.ExcludeFileTypes?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetExcludeDirs()
        {
            return this.ExcludeDirs?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
    
    /// <remarks/>
    [Serializable]
    public class Options
    {
        public bool CreateLogFile { get; set; } = false;

        public bool Verify { get; set; } = false;
        
        public string GlobalExcludeFileTypes { get; set; }

        public string GlobalExcludeDirs { get; set; }


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
