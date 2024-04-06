using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using ThrowException.CSharpLibs.BytesUtilLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class AdminMailer
    {
        private readonly ILogger _logger;
        private readonly Mailer _mailer;
        private readonly MailerConfig _config;
        private Dictionary<Guid, DateTime> _db;

        public AdminMailer(ILogger logger, Mailer mailer, MailerConfig config)
        {
            _logger = logger;
            _mailer = mailer;
            _config = config;
            _db = new Dictionary<Guid, DateTime>();
        }

        private Guid CreateId(string subject, string message, params object[] args)
        {
            using (var hash = SHA256.Create())
            {
                return new Guid(hash.ComputeHash(Encoding.UTF8.GetBytes(subject + "$$$" + string.Format(message, args))).Part(0, 16));
            }
        }

        public void Process()
        {
            var removes = _db.Where(e => DateTime.Now.Subtract(e.Value) < _config.RepeatTime).ToList();

            foreach (var entry in removes)
            {
                _db.Remove(entry.Key);
            }
        }

        public void Send(string subject, string message, params object[] args)
        {
            var id = CreateId(subject, message, args);

            if ((!_db.ContainsKey(id)) || (DateTime.Now.Subtract(_db[id]) >= _config.RepeatTime))
            {
                _logger.Info("New admin mail subject {0}", subject);
                _mailer.Send(_config.AdminName, _config.AdminAddress, subject, message, args);
                if (_db.ContainsKey(id))
                {
                    _db[id] = DateTime.Now;
                }
                else
                {
                    _db.Add(id, DateTime.Now);
                }
            }
        }

        public void Send(string subject, Exception exception)
        {
            Send(subject, exception.ToString());
        }
    }
}
