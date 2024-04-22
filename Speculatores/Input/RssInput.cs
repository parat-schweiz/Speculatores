using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using CodeHollow.FeedReader;
using Npgsql;
using ThrowException.CSharpLibs.BytesUtilLib;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class RssInputConfig : IConfig, IBackoffConfig
    {
        [Setting]
        public string Name { get; private set; }

        [Setting]
        public string Url { get; private set; }

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

    public class RssInput : IInput
    {
        private readonly ILogger _logger;
        private readonly AdminMailer _admin;
        private readonly RssInputConfig _config;
        private readonly NpgsqlConnection _db;
        private readonly List<IFilter> _filters;
        private readonly IdTable _idTable;
        private readonly Backoff _backoff;

        public string Name { get { return _config.Name; } }

        public RssInput(ILogger logger, AdminMailer admin, RssInputConfig config, NpgsqlConnection db)
        {
            _logger = logger;
            _admin = admin;
            _config = config;
            _db = db;
            _filters = new List<IFilter>();
            _idTable = new IdTable(_db, "rssinputdata");
            _backoff = new Backoff(_logger, _config);
        }

        private Guid CreateId(string text)
        {
            using (var hash = SHA256.Create())
            {
                return new Guid(hash.ComputeHash(Encoding.UTF8.GetBytes(text)).Part(0, 16));
            }
        }

        public void Process()
        {
            if (_backoff.Check())
            {
                try
                {
                    var feed = FeedReader.ReadAsync(_config.Url).Result;
                    var pubsReturned = 0;
                    var pubsOverage = 0;
                    var pubsNotLoaded = 0;
                    var pubsPending = 0;
                    _logger.Debug("RSS input {0} has {1} items", Name, feed.Items.Count);

                    foreach (var item in feed.Items)
                    {
                        if (DateTime.Now.Subtract(item.PublishingDate ?? DateTime.Now) <= _config.MaxAge)
                        {
                            var text = _config.Format
                                .Replace("{title}", item.Title)
                                .Replace("{content}", item.Content)
                                .Replace("{description}", item.Description)
                                .Replace("{link}", item.Link)
                                .Replace("{nn}", "\n\n")
                                .Replace("{n}", "\n");
                            Guid id = CreateId(text);

                            if (_idTable.NotContained(id))
                            {
                                if (pubsReturned < _config.MaxResults)
                                {
                                    _logger.Info("New message in RSS {0}: {1}", Name, text.Substring(0, Math.Min(30, text.Length)).Replace("\n", " "));
                                    _logger.Verbose("New message in RSS {0}: {1}", Name, text);

                                    _idTable.Insert(id);

                                    foreach (var filter in _filters)
                                    {
                                        filter.Process(new Message(id, text));
                                    }
                                    pubsReturned++;
                                }
                                else
                                {
                                    pubsPending++;
                                }
                            }
                            else
                            {
                                pubsNotLoaded++;
                            }
                        }
                        else
                        {
                            pubsOverage++;
                        }
                    }
                    _logger.Debug("RSS inputs overage {0} not loaded {1} pending {2} returned {3}", pubsOverage, pubsNotLoaded, pubsPending, pubsReturned);
                    _backoff.Success();
                }
                catch (Exception exception)
                {
                    if (_backoff.FailureCount < 2)
                    {
                        _logger.Warning("RSS input error: " + exception.Message);
                        _logger.Debug(exception.ToString());
                    }
                    else
                    {
                        _logger.Error("RSS input error: " + exception.Message);
                        _logger.Debug(exception.ToString());
                        _admin.Send(string.Format("RSS input {0} failed", Name), exception);
                    }
                    _backoff.Failure();
                }
            }
        }

        public void Add(IFilter filter)
        {
            _filters.Add(filter);
        }
    }
}
