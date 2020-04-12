using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Angular.Hubs;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;
using SignalRTypeScriptHubGenerator;

namespace Angular
{
    public static class ReinforcedTypingsConfiguration
    {
        public static void Configure(ConfigurationBuilder builder)
        {
            builder.Global(c =>
            {
                c.CamelCaseForMethods();
                c.CamelCaseForProperties();
            });

            

            builder.GenerateSignalRTypeScriptHub(new HubConnectionProviderReference("{ HubConnectionProvider } ", "./hubconnectionprovider.service"), typeof(WeatherForecastHub), "/hub", "Angular");
            //builder.Substitute(typeof(Task<List<WeatherForecast>>), new RtSimpleTypeName("Promise<WeatherForecast[]>"));
            //builder.ExportAsClass<WeatherForecast>();
            //builder.SubstituteGeneric(typeof(Task<>), (type, resolver) => new RtSimpleTypeName("Promise", resolver.ResolveTypeName(type.GetGenericArguments()[0])));
        }
    }
}
