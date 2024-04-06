using System;
using System.Collections.Generic;
using ThrowException.CSharpLibs.ConfigParserLib;

namespace Speculatores
{
    public class InputConfig : IConfig
    {
        [Setting]
        public IEnumerable<RssInputConfig> Rss { get; private set; }

        [Setting]
        public IEnumerable<AmtsblattInputConfig> Amtsblatt { get; private set; }
    }

    public interface IInput
    {
        string Name { get; }

        void Add(IFilter filter);

        void Process();
    }
}
