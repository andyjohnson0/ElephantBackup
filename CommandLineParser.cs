using System;
using System.Collections.Generic;


namespace uk.andyjohnson.ElephantBackup
{
    public class CommandLineParser
    {
        public CommandLineParser(string[] argValues)
        {
            if (argValues == null)
                throw new ArgumentNullException("argValues");
            this.argValues = argValues;
        }

        private string[] argValues;


        //public bool GetArg(string argName, int argIdx)
        //{
        //    if (argValues.Length > argIdx)
        //    {
        //        if ((argValues[argIdx] == ("/" + argName)) || (argValues[argIdx] == ("-" + argName)))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}


        public string[] GetArgValues(string argName)
        {
            var v = new List<string>();

            for (int i = 0; i < (argValues.Length - 1); i++)
            {
                if ((argValues[i] == ("/" + argName)) || (argValues[i] == ("-" + argName)))
                {
                    if (!argValues[i + 1].StartsWith("/") && !argValues[i + 1].StartsWith("-"))
                    {
                        v.Add(argValues[i + 1]);
                        i += 1;
                    }
                }
            }
            return v.ToArray();
        }



        public string GetArgValue(string argName, string defaultValue = null)
        {
            for (int i = 0; i < (argValues.Length - 1); i++)
            {
                if ((argValues[i] == ("/" + argName)) || (argValues[i] == ("-" + argName)))
                {
                    return argValues[i + 1];
                }
            }
            return defaultValue;
        }


        public bool GetArg(string argName)
        {
            foreach (string arg in argValues)
            {
                if ((arg == ("/" + argName)) || (arg == ("-" + argName)))
                {
                    return true;
                }
            }
            return false;
        }


        public bool GetArg(string[] argNames)
        {
            foreach (var argName in argNames)
            {
                if (GetArg(argName))
                    return true;
            }
            return false;
        }
    }
}