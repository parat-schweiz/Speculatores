using System;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;
using MailKit.Net.Smtp;
using MimeKit;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace Speculatores
{
    public class MailerConfig : IConfig, IBackoffConfig
    {
        [Setting]
        public string ServerAddress { get; private set; }

        [Setting]
        public int ServerPort { get; private set; }

        [Setting]
        public string Username { get; private set; }

        [Setting]
        public string Password { get; private set; }

        [Setting]
        public string SenderName { get; private set; }

        [Setting]
        public string SenderAddress { get; private set; }

        [Setting]
        public string AdminName { get; private set; }

        [Setting]
        public string AdminAddress { get; private set; }

        [Setting]
        public TimeSpan RepeatTime { get; private set; }

        [Setting]
        public TimeSpan BackoffTime { get; private set; }

        [Setting]
        public int BackoffMaxFactor { get; private set; }

        public string Name { get { return "mailer"; } }
    }

    public class Mailer
    {
        private readonly ILogger _logger;
        private readonly MailerConfig _config;
        private readonly MailQueue _queue;
        private readonly Backoff _backoff;

        public Mailer(ILogger logger, NpgsqlConnection db, MailerConfig config)
        {
            _logger = logger;
            _config = config;
            _queue = new MailQueue(db, "mailqueue");
            _backoff = new Backoff(_logger, _config);
        }

        public void Process()
        {
            if (_queue.Any())
            {
                _logger.Debug("Mailer has {0} messages queued", _queue.Count());
                TrySend(_queue.Dequeue());
            }
        }

        public void Send(string toName, string toAddress, string subject, string text, params object[] args)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.SenderName, _config.SenderAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;
            var body = new TextPart(MimeKit.Text.TextFormat.Plain);
            body.Text = string.Format(text, args); ;
            message.Body = body;
            TrySend(message);
        }

        private void TrySend(MimeMessage message)
        {
            if (_backoff.Check())
            {
                try
                {
                    Send(message);
                    _backoff.Success();
                }
                catch (Exception exception)
                {
                    _logger.Error("Mailer eror: " + exception.Message);
                    _logger.Verbose(exception.ToString());
                    _queue.Enqueue(message);
                    _backoff.Failure();
                }
            }
            else
            {
                _queue.Enqueue(message);
            }
        }

        private void Send(MimeMessage message)
        {
            var client = new SmtpClient();
            client.Connect(_config.ServerAddress, _config.ServerPort);
            client.Authenticate(_config.Username, _config.Password);
            client.Send(message);
        }
    }
}
