using System;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public interface IBackoffConfig
    {
        string Name { get; }
        TimeSpan BackoffTime { get; }
        int BackoffMaxFactor { get; }
    }

    public class Backoff
    {
        private readonly ILogger _logger;
        private readonly IBackoffConfig _config;
        private int _failureCount;
        private DateTime _until;

        public Backoff(ILogger logger, IBackoffConfig config)
        {
            _logger = logger;
            _config = config;
            _failureCount = 0;
            _until = DateTime.MinValue;
        }

        public bool Check()
        {
            if (DateTime.Now > _until)
            {
                return true;
            }
            else
            {
                _logger.Debug("{0} is in backoff until {1}", _config.Name, _until);
                return false;
            }
        }

        public void Success()
        {
            if (_failureCount > 0)
            {
                _logger.Debug("{0} succeded and reset failure count", _config.Name);
            }
            _failureCount = 0;
            _until = DateTime.MinValue;
        }

        public void Failure()
        {
            var totalBackoff = new TimeSpan(_config.BackoffTime.Ticks * Math.Max(_config.BackoffMaxFactor, _failureCount));
            _until = DateTime.Now.Add(totalBackoff);
            _failureCount++;
            _logger.Debug("{0} failure set backoff of {1} until {2} failure count {3}", _config.Name, totalBackoff, _until, _failureCount);
        }
    }
}
