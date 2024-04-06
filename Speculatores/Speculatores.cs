using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class Speculatores : IDisposable
    {
        private readonly ILogger _logger;
        private readonly SpeculatoresConfig _config;
        private readonly Mailer _mailer;
        private readonly NpgsqlConnection _db;
        private readonly AdminMailer _admin;
        private readonly List<IInput> _inputs;
        private readonly List<IOutput> _outputs;
        private readonly List<IFilter> _filters;

        private IEnumerable<IInput> CreateInputs()
        { 
            foreach (var rss in _config.Input.Rss)
            {
                yield return new RssInput(_logger, _admin, rss, _db);
            }

            foreach (var amtsblatt in _config.Input.Amtsblatt)
            {
                yield return new AmtsblattInput(_logger, _admin, amtsblatt, _db);
            }
        }

        private IEnumerable<IOutput> CreateOutputs()
        {
            foreach (var mastodon in _config.Output.Mastodon)
            {
                yield return new MastodonOutput(_logger, _db, _admin, mastodon);
            }

            foreach (var mail in _config.Output.Mail)
            {
                yield return new MailOutput(_logger, _mailer, mail);
            }

            foreach (var matrix in _config.Output.Matrix)
            {
                yield return new MatrixOutput(_logger, _db, _admin, matrix);
            }
        }

        private IEnumerable<IFilter> CreateFilters()
        {
            foreach (var regex in _config.Filter.Regex)
            {
                yield return new RegexFilter(_logger, regex, _inputs, _outputs);
            }
        }

        public Speculatores(ILogger logger, SpeculatoresConfig config)
        {
            _logger = logger;
            _config = config;

            _db = new NpgsqlConnection(_config.DatabaseConnectionString);
            _db.Open();

            _mailer = new Mailer(_logger, _db, _config.Mailer);

            _admin = new AdminMailer(_logger, _mailer, _config.Mailer);

            _inputs = CreateInputs().ToList();
            _outputs = CreateOutputs().ToList();
            _filters = CreateFilters().ToList();

            _logger.Info("Speculatores loaded");
        }

        public void Process()
        {
            _logger.Debug("Processing");
            _admin.Process();
            _mailer.Process();

            foreach (var output in _outputs)
            {
                output.Process();
            }

            foreach (var input in _inputs)
            {
                input.Process();
            }
        }

        public void Dispose()
        {
            _db.Close();
        }
    }
}
