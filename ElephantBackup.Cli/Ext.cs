using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.ElephantBackup.Lib
{
    public static class LongExt
    {
        public static string ToByteQuantity(this long value)
        {
            var labels = new string[]
            {
                "bytes", "KB", "MB", "GB", "TB"
            };
            
            for(int i = labels.Length - 1; i >= 0; i--)
            {
                var v = (double)value / Math.Pow(1024D, (double)i);
                if ((v > 1D) || (i == 0))
                {
                    return string.Format("{0:.00} {1}", v, labels[i]);
                }
            }

            // Not reached.
            return "";
        }
    }
}
