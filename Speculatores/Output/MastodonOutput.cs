using System;
using System.Collections.Generic;
using System.Linq;
using Mastonet;
using Npgsql;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class MastodonOutputConfig : IConfig, IBackoffConfig
    {
        [Setting]
        public string Name { get; private set; }

        [Setting]
        public string Url { get; private set; }

        [Setting]
        public string AccessToken { get; private set; }

        [Setting]
        public TimeSpan BackoffTime { get; private set; }

        [Setting]
        public int BackoffMaxFactor { get; private set; }
    }

    public class MastodonOutput : IOutput
    {
        private readonly ILogger _logger;
        private readonly AdminMailer _admin;
        private MastodonOutputConfig _config;
        private MastodonClient _client;
        private MessageBuffer _queue;
        private readonly Backoff _backoff;

        public string Name { get { return _config.Name; } }

        private string Shorten(string text)
        {
            if (text.Length > 500)
            {
                var modText = text
                    .Trim()
                    .Replace("\n\n", " \n\n ")
                    .Replace("\n", " \n ");
                var words = new Queue<string>(modText
                    .Split(new string[] { " " }, StringSplitOptions.None).Reverse());
                var newWords = new List<string>();
                var length = text.Length;
                var addeddots = false;
                while (words.Any())
                {
                    var word = words.Dequeue();
                    if (word.StartsWith("http://", StringComparison.InvariantCulture) ||
                        word.StartsWith("http://", StringComparison.InvariantCulture))
                    {
                        newWords.Add(word);
                    }
                    else if (length > 500)
                    {
                        if (!addeddots)
                        {
                            newWords.Add("...");
                            length += 4;
                            addeddots = true;
                        }
                        length -= (word.Length + 1);
                    }
                    else
                    {
                        newWords.Add(word);
                    }
                }
                newWords.Reverse();
                var newText = string.Join(" ", newWords);

                if ((newText.Length < 1) || (newText.Length > 500))
                {
                    return text.Substring(0, Math.Min(500, text.Length));
                }
                else
                {
                    return newText
                        .Replace(" \n\n ", "\n\n")
                        .Replace(" \n\n", "\n\n")
                        .Replace(" \n ", "\n")
                        .Replace(" \n", "\n")
                        .Trim();
                }
            }
            else
            {
                return text;
            }
        }

        public MastodonOutput(ILogger logger, NpgsqlConnection db, AdminMailer admin, MastodonOutputConfig config)
        {
            _logger = logger;
            _admin = admin;
            _config = config;
            _backoff = new Backoff(_logger, _config);
            _queue = new MessageBuffer(db, "mastodonoutput", Name);
            _client = new MastodonClient(config.Url, config.AccessToken);
        }

        public void Send(Message message)
        {
            if (_backoff.Check())
            {
                try
                {
                    var text = Shorten(message.Text);
                    _logger.Info("Publishing to mastodon {0}: {1}", Name, text.Substring(0, Math.Min(30, text.Length)).Replace("\n", " "));
                    _logger.Verbose("Publishing to mastodon {0}: {1}", Name, text);
                    _client.PublishStatus(text).Wait();
                    _backoff.Success();
                }
                catch (Exception exception)
                {
                    _logger.Error("Mastodon error: " + exception.Message);
                    _logger.Debug(exception.ToString());
                    _admin.Send(string.Format("Mastodon {0} failed", Name), exception);
                    _queue.Insert(message);
                    _backoff.Failure();
                }
            }
            else
            {
                _queue.Insert(message);
            }
        }

        public void Process()
        {
            var message = _queue.Select(1).SingleOrDefault();
            if (message != null)
            {
                _logger.Debug("Mastodon output {0} queue length {1}", Name, _queue.Count());
                _queue.Delete(message.Id);
                Send(message);
            }
        }
    }
}
