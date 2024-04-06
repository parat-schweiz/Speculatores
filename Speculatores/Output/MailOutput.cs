using System;
using System.Collections.Generic;
using System.Linq;
using Mastonet;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class MailOutputConfig : IConfig
    {
        [Setting]
        public string Name { get; private set; }

        [Setting]
        public string ToName { get; private set; }

        [Setting]
        public string ToAddress { get; private set; }

        [Setting]
        public string Subject { get; private set; }
    }

    public class MailOutput : IOutput
    {
        private readonly ILogger _logger;
        private readonly MailOutputConfig _config;
        private readonly Mailer _mailer;

        public string Name { get { return _config.Name; } }

        public MailOutput(ILogger logger, Mailer mailer, MailOutputConfig config)
        {
            _logger = logger;
            _mailer = mailer;
            _config = config;
        }

        public void Send(Message message)
        {
            _logger.Info("Sending mail to {0}: {1}", Name, message.Text.Substring(0, Math.Min(30, message.Text.Length)).Replace("\n", " "));
            _logger.Verbose("Sending mail to {0}: {1}", Name, message.Text);
            _mailer.Send(_config.ToName, _config.ToAddress, _config.Subject, message.Text);
        }

        public void Process()
        {
        }
    }
}
