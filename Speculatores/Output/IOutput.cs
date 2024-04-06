using System;
using System.Collections.Generic;
using ThrowException.CSharpLibs.ConfigParserLib;

namespace Speculatores
{
    public class OutputConfig : IConfig
    { 
        [Setting]
        public IEnumerable<MastodonOutputConfig> Mastodon { get; private set; }

        [Setting]
        public IEnumerable<MailOutputConfig> Mail { get; private set; }

        [Setting]
        public IEnumerable<MatrixOutputConfig> Matrix { get; private set; }
    }

    public interface IOutput
    {
        string Name { get; }

        void Send(Message message);

        void Process();
    }
}
