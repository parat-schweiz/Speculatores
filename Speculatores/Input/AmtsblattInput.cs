using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class AmtsblattInputConfig : IConfig, IBackoffConfig
    {
        [Setting]
        public string Name { get; private set; }

        [Setting(Required = false, DefaultValue = null)]
        public string Tenant { get; private set; }

        [Setting(Required = false, DefaultValue = null)]
        public string Rubric { get; private set; }

        [Setting(Required = false, DefaultValue = null)]
        public string SubRubric { get; private set; }

        [Setting(Required = false, DefaultValue = null)]
        public string TitlePattern { get; private set; }

        [Setting(Required = false, DefaultValue = null)]
        public string TextPattern { get; private set; }

        [Setting]
        public string Format { get; private set; }

        [Setting]
        public TimeSpan MaxAge { get; private set; }

        [Setting]
        public int MaxResults { get; private set; }

        [Setting]
        public TimeSpan BackoffTime { get; private set; }

        [Setting]
        public int BackoffMaxFactor { get; private set; }
    }

    public class AmtsblattInput : IInput
    {
        private readonly ILogger _logger;
        private readonly AdminMailer _admin;
        private readonly AmtsblattInputConfig _config;
        private readonly NpgsqlConnection _db;
        private readonly List<IFilter> _filters;
        private readonly IdTable _idTable;
        private readonly Backoff _backoff;

        public AmtsblattInput(ILogger logger, AdminMailer admin, AmtsblattInputConfig config, NpgsqlConnection db)
        {
            _logger = logger;
            _config = config;
            _admin = admin;
            _db = db;
            _filters = new List<IFilter>();
            _idTable = new IdTable(_db, "amtsblattdata");
            _backoff = new Backoff(_logger, _config);
        }

        public string Name { get { return _config.Name; } }

        public void Add(IFilter filter)
        {
            _filters.Add(filter);
        }

        public void Process()
        {
            if (_backoff.Check())
            {
                try
                {
                    var reader = new AmtsblattReader(_logger);
                    var list = reader.Get(_config.MaxResults, _config.MaxAge, _config.Tenant, _config.Rubric, _config.SubRubric, _idTable.NotContained).ToList();
                    foreach (var entry in list)
                    {
                        bool match = true;
                        if (!string.IsNullOrEmpty(_config.TitlePattern))
                        {
                            match &= Regex.IsMatch(entry.Title, _config.TitlePattern);
                        }
                        if (!string.IsNullOrEmpty(_config.TextPattern))
                        {
                            match &= Regex.IsMatch(entry.Text, _config.TextPattern);
                        }
                        var text = _config.Format
                            .Replace("{date}", entry.Date.ToString("dd.MM.yyyy"))
                            .Replace("{office}", entry.Office)
                            .Replace("{title}", entry.Title)
                            .Replace("{text}", entry.Text)
                            .Replace("{url}", entry.Urls.FirstOrDefault() ?? string.Empty)
                            .Replace("{urls}", string.Join(" ", entry.Urls))
                            .Replace("{urlsn}", string.Join("\n", entry.Urls))
                            .Replace("{nn}", "\n\n")
                            .Replace("{n}", "\n");
                        _logger.Info("New message in amtsblatt {0}: {1}", Name, text.Substring(0, Math.Min(30, text.Length)).Replace("\n", " "));
                        _logger.Verbose("New message in amtsblatt {0}: {1}", Name, text);
                        foreach (var filter in _filters)
                        {
                            filter.Process(new Message(entry.Id, text));
                        }
                        _idTable.Insert(entry.Id);
                    }
                    _backoff.Success();
                }
                catch (Exception exception)
                {
                    if (_backoff.FailureCount < 2)
                    {
                        _logger.Warning("Amtsblatt input error: " + exception.Message);
                        _logger.Debug(exception.ToString());
                    }
                    else
                    {
                        _logger.Error("Amtsblatt input error: " + exception.Message);
                        _logger.Debug(exception.ToString());
                        _admin.Send("Amtsblatt input error", exception);
                    }
                    _backoff.Failure();
                }
            }
        }
    }
}
