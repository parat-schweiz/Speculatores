using System;
using System.IO;
using System.Linq;
using System.Threading;
using CodeHollow.FeedReader;
using Matrix.Sdk;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Missing config file argument");
                Environment.Exit(1);
            }

            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("Config file not found");
                Environment.Exit(2);
            }

            var parser = new XmlConfig<SpeculatoresConfig>();
            var config = parser.ParseFile(args[0]);

            var logger = new Logger();
            logger.ConsoleSeverity = config.ConsoleLogLevel;

            using (var speculatores = new Speculatores(logger, config))
            {
                while (true)
                {
                    speculatores.Process();
                    Thread.Sleep((int)config.ProcessTime.TotalMilliseconds);
                }
            }
        }
    }
}
