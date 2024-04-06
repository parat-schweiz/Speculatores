using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThrowException.CSharpLibs.ConfigParserLib;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class RegexFilterConfig : IConfig
    {
        [Setting]
        public IEnumerable<string> InputName { get; private set; }

        [Setting]
        public IEnumerable<string> OutputName { get; private set; }

        [Setting]
        public IEnumerable<string> Pattern { get; private set; }
    }

    public class RegexFilter : IFilter
    {
        private readonly ILogger _logger;
        private readonly RegexFilterConfig _config;
        private readonly IEnumerable<IOutput> _outputs;

        public RegexFilter(ILogger logger, RegexFilterConfig config, IEnumerable<IInput> inputs, IEnumerable<IOutput> outputs)
        {
            _logger = logger;
            _config = config;

            foreach (var name in _config.InputName)
            {
                inputs.Single(i => i.Name == name).Add(this);
            }

            _outputs = _config.OutputName
                .Select(name => outputs.Single(o => o.Name == name))
                .ToList();
        }

        public void Process(Message message)
        {
            if (_config.Pattern.Any(p => Regex.IsMatch(message.Text, p)))
            { 
                foreach (var output in _outputs)
                {
                    output.Send(message);
                }
            }
        }
    }
}
