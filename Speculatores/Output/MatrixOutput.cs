using System;
using System.Collections.Generic;
using System.Linq;
using Matrix.Sdk;
using Npgsql;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class MatrixOutputConfig : IConfig, IBackoffConfig
    {
        [Setting]
        public string Name { get; private set; }

        [Setting]
        public string ServerUrl { get; private set; }

        [Setting]
        public string Username { get; private set; }

        [Setting]
        public string Password { get; private set; }

        [Setting]
        public TimeSpan BackoffTime { get; private set; }

        [Setting]
        public int BackoffMaxFactor { get; private set; }
    }

    public class MatrixOutput : IOutput
    {
        private readonly MatrixOutputConfig _config;
        private readonly AdminMailer _admin;
        private readonly ILogger _logger;
        private readonly MessageBuffer _queue;
        private readonly Backoff _backoff;
        private IMatrixClient _client;

        public MatrixOutput(ILogger logger, NpgsqlConnection db, AdminMailer admin, MatrixOutputConfig config)
        {
            _logger = logger;
            _admin = admin;
            _config = config;
            _queue = new MessageBuffer(db, "matrixoutput", Name);
            _backoff = new Backoff(_logger, _config);
        }

        public string Name { get { return _config.Name; } }

        private void EnsureLogin()
        {
            if (_client == null)
            {
                var factory = new MatrixClientFactory();
                _client = factory.Create();
                _client.LoginAsync(new Uri(_config.ServerUrl), _config.Username, _config.Password, "speculatores").Wait();
            }
        }

        public void Process()
        {
            var message = _queue.Select(1).SingleOrDefault();
            if (message != null)
            {
                _logger.Debug("Matrix output {0} queue length {1}", Name, _queue.Count());
                _queue.Delete(message.Id);
                TrySend(message);
            }
        }

        public void Send(Message message)
        {
            TrySend(message);
        }

        private void TrySend(Message message)
        {
            if (_backoff.Check())
            {
                try
                {
                    EnsureLogin();
                    SendInternal(message);
                    _backoff.Success();
                }
                catch (Exception exception)
                {
                    if (_backoff.FailureCount < 2)
                    {
                        _logger.Warning("Matrix output {0} error: {1}", Name, exception.Message);
                        _logger.Verbose("Matrix output error: {1}", exception.ToString());
                    }
                    else
                    {
                        _logger.Error("Matrix output {0} error: {1}", Name, exception.Message);
                        _logger.Verbose("Matrix output error: {1}", exception.ToString());
                        _admin.Send("Matrix output {0} error", exception);
                    }
                    _queue.Insert(message);
                    _client = null;
                    _backoff.Failure();
                }
            }
            else
            {
                _queue.Insert(message);
            }
        }

        private void SendInternal(Message message)
        {
            var rooms = _client.GetJoinedRoomsIdsAsync().Result;
            foreach (var room in rooms)
            {
                _client.SendMessageAsync(room, message.Text);
            }
        }
    }
}
