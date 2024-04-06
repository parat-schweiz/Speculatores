using System;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class SpeculatoresConfig : IConfig
    {
        [Setting]
        public TimeSpan ProcessTime { get; private set; }

        [Setting]
        public LogSeverity ConsoleLogLevel { get; private set; }

        [Setting]
        public string DatabaseConnectionString { get; private set; }

        [Setting]
        public InputConfig Input { get; private set; }

        [Setting]
        public OutputConfig Output { get; private set; }

        [Setting]
        public FilterConfig Filter { get; private set; }

        [Setting]
        public MailerConfig Mailer { get; private set; }
    }
}
