using System;
using System.Collections.Generic;
using ThrowException.CSharpLibs.ConfigParserLib;

namespace Speculatores
{
    public class FilterConfig : IConfig
    {
        [Setting]
        public IEnumerable<RegexFilterConfig> Regex { get; private set; }
    }

    public interface IFilter
    {
        void Process(Message message);
    }
}
