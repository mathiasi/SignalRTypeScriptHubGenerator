using System;
using System.Collections.Generic;
using System.Text;

namespace SignalRTypeScriptHubGenerator
{
    public class HubConnectionProviderReference
    {
        public string Target { get; }
        public string From { get;}

        public HubConnectionProviderReference(string target, string from)
        {
            Target = target;
            From = from;
        }
    }
}
