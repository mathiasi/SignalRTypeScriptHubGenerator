using System;
using System.Threading.Tasks;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;

namespace SignalRTypeScriptGenerator
{
    public static class SignalRTypeScriptGeneratorExtensions
    {
        public static void GenerateSignalRTypeScript(this ConfigurationBuilder builder)
        {
            builder.AddImport("{ Injectable }", "@angular/core");
            builder.AddImport("{ HubConnection }", "@microsoft/signalr");
            builder.AddImport("* as signalR", "@microsoft/signalr");
            builder.AddImport("{ SignalDispatcher, SimpleEventDispatcher }", "strongly-typed-events");
            builder.AddImport("{ HubConnectionProvider }", "./hubconnectionprovider.service");

            builder.Substitute(typeof(Task), new RtSimpleTypeName("Promise<void>"));
            builder.Substitute(typeof(Task<>), new RtSimpleTypeName("Promise"));
        }
    }
}
