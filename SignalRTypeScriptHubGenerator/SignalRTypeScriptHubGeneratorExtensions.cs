﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Reinforced.Typings;
using Reinforced.Typings.Ast;
using Reinforced.Typings.Ast.TypeNames;
using Reinforced.Typings.Fluent;
using Reinforced.Typings.Generators;

namespace SignalRTypeScriptHubGenerator
{
    public static class SignalRTypeScriptHubGeneratorExtensions
    {
        public static void GenerateSignalRTypeScriptHub(this ConfigurationBuilder builder, HubConnectionProviderReference hubConnectionProviderReference, Type hub, string hubPattern, string namespaceFilter)
        {
            var serverType = hub;
            var frontendType = hub.BaseType.GetGenericArguments()[0];

            builder.Global(c => c.UseModules());

            builder.AddImport("{ Injectable }", "@angular/core");
            builder.AddImport("{ HubConnection }", "@microsoft/signalr");
            builder.AddImport("{ SignalDispatcher, SimpleEventDispatcher }", "strongly-typed-events");
            builder.AddImport(hubConnectionProviderReference.Target, hubConnectionProviderReference.From);

            builder.Substitute(typeof(Task), new RtSimpleTypeName("Promise<void>"));
            builder.Substitute(typeof(Task<>), new RtSimpleTypeName("Promise"));
            builder.Substitute(typeof(DateTime), new RtSimpleTypeName("Date"));
            builder.Substitute(typeof(Uri), new RtSimpleTypeName("string"));

            var relatedTypes = new HashSet<Type>();
            relatedTypes.UnionWith(TraverseTypes(serverType, namespaceFilter));
            relatedTypes.UnionWith(TraverseTypes(frontendType, namespaceFilter));
            relatedTypes.Remove(serverType);
            relatedTypes.Remove(frontendType);

            var relatedClassTypes = relatedTypes.Where(t => t.IsClass);
            var relatedEnumTypes = relatedTypes.Where(t => t.IsEnum);
            var otherTypes = relatedTypes.Where(t => !(t.IsClass || t.IsEnum));

            Func<Type, int> orderFunc = relatedTypes.SelectMany(t => EnumerateHierarchy(t, e => e.BaseType).Reverse()).ToList().IndexOf;

            builder.ExportAsEnums(relatedEnumTypes, c => c.Order(orderFunc(c.Type)));
            builder.ExportAsClasses(relatedClassTypes, c => c.WithPublicProperties().WithPublicFields().WithPublicMethods().Order(orderFunc(c.Type)));
            builder.ExportAsInterfaces(otherTypes, c => c.WithPublicProperties().WithPublicFields().WithPublicMethods().Order(orderFunc(c.Type)));
            builder.ExportAsInterfaces(new[] {serverType}, c => c.WithPublicProperties().WithPublicFields().WithPublicMethods().WithCodeGenerator<ServerClientAppender>());
            builder.ExportAsInterfaces(new[] {frontendType}, c => c.WithPublicProperties().WithPublicFields().WithPublicMethods().WithCodeGenerator<FrontEndClientAppender>());
        }

        private static IEnumerable<T> EnumerateHierarchy<T>(T item, Func<T, T> selector) where T : class
        {
            do
            {
                yield return item;
                item = selector(item);
            }
            while (item != default(T));
        }

        public static IEnumerable<Type> TraverseTypes(Type type, string namespaceFilter)
        {
            return TraverseTypes(type, namespaceFilter, new List<Type>());
        }

        public static IEnumerable<Type> TraverseTypes(Type type, string namespaceFilter, List<Type> visited)
        {
            visited.Add(type);
            var types = new HashSet<Type>();
            types.UnionWith(type.GetInterfaces());
            types.UnionWith(GetBaseClassIfAny(type));
            types.UnionWith(TraverseMethods(type));
            types.UnionWith(TraverseProperties(type));
            types.UnionWith(types.ToList().SelectMany(t => visited.Contains(t) ? new List<Type>() : TraverseTypes(t, namespaceFilter, visited)));
            types.IntersectWith(types.Where(t => t.Namespace != null && t.Namespace.StartsWith(namespaceFilter)));
            types.ExceptWith(types.ToList().Where(t => t.IsByRef || t.IsArray));
            types.Add(type);
            return types;
        }

        private static IEnumerable<Type> GetBaseClassIfAny(Type type)
        {
            return type.BaseType == null ? new Type[0] : new[] { type.BaseType };
        }

        private static IEnumerable<Type> TraverseProperties(Type type)
        {
            return type.GetProperties().SelectMany(HandleProperty);
        }

        private static IEnumerable<Type> TraverseMethods(Type type)
        {
            var types = new HashSet<Type>();
            types.UnionWith(type.GetMethods().SelectMany(HandleReturnType));
            types.UnionWith(type.GetMethods().SelectMany(m => m.GetParameters().SelectMany(HandleParameters)));
            return types;
        }

        private static IEnumerable<Type> HandleParameters(ParameterInfo p)
        {
            var types = new HashSet<Type> { p.ParameterType };
            if (p.ParameterType.IsGenericType)
            {
                types.UnionWith(p.ParameterType.GetGenericArguments());
            }

            return types;
        }

        private static IEnumerable<Type> HandleReturnType(MethodInfo m)
        {
            var types = new HashSet<Type> { m.ReturnType };
            if (m.ReturnType.IsGenericType)
            {
                types.UnionWith(m.ReturnType.GetGenericArguments());
            }

            return types;
        }

        private static IEnumerable<Type> HandleProperty(PropertyInfo p)
        {
            var types = new HashSet<Type> { p.PropertyType };
            if (p.PropertyType.IsGenericType)
            {
                types.UnionWith(p.PropertyType.GetGenericArguments());
            }

            return types;
        }
    }

    internal abstract class ClientAppenderBase : InterfaceCodeGenerator
    {
        public override RtInterface GenerateNode(Type element, RtInterface result, TypeResolver resolver)
        {
            var existing = base.GenerateNode(element, result, resolver);
            if (existing == null) return null;

            if (Context.Location.CurrentNamespace == null) return existing;

            ClientAppenderImpl(element, result, resolver);

            return ReturnExisting() ? existing : null;
        }

        protected virtual bool ReturnExisting()
        {
            return true;
        }

        protected abstract void ClientAppenderImpl(Type element, RtInterface result, TypeResolver resolver);
    }

    internal class ServerClientAppender : ClientAppenderBase
    {
        protected override void ClientAppenderImpl(Type element, RtInterface result, TypeResolver resolver)
        {
            var clientImpl = new RtClass()
            {
                Name = new RtSimpleTypeName($"{element.Name}Client"),
                Export = true,
                Decorators = { new RtDecorator("Injectable()\r\n") },
                Implementees = { result.Name },
                Members =
                {
                    new RtField
                    {
                        AccessModifier = AccessModifier.Private,
                        Identifier = new RtIdentifier("hubConnection"),
                        Type = new RtSimpleTypeName("Promise<HubConnection>")
                    },
                    new RtConstructor
                    {
                        Arguments = { new RtArgument
                        {
                            Type = new RtSimpleTypeName("HubConnectionProvider"),
                            Identifier = new RtIdentifier("hubConnectionProvider")
                        }},
                        Body = new RtRaw("this.hubConnection = hubConnectionProvider.getHubConnection(\"/hub\");"),
                    }
                },
            };
            clientImpl.Members.AddRange(GetImplementationMembers(result));

            Context.Location.CurrentNamespace.CompilationUnits.Add(clientImpl);
        }

        private IEnumerable<RtNode> GetImplementationMembers(RtInterface result)
        {
            var functions = result.Members.OfType<RtFuncion>();
            foreach (var function in functions)
            {
                var arguments = function.Arguments.Select(a => a.Identifier.ToString());
                function.Body = new RtRaw($"return this.hubConnection.then(hub => hub.invoke(\"{function.Identifier.IdentifierName}\",{string.Join(",", arguments)}));");
            }

            return functions;
        }
    }

    internal class FrontEndClientAppender : ClientAppenderBase
    {
        protected override bool ReturnExisting()
        {
            return false;
        }

        protected override void ClientAppenderImpl(Type element, RtInterface result, TypeResolver resolver)
        {
            var typeName = element.IsInterface && element.Name.StartsWith('I') ? element.Name.Substring(1) : element.Name;
            var clientImpl = new RtClass
            {
                Name = new RtSimpleTypeName(typeName),
                Export = true,
                Decorators = { new RtDecorator("Injectable()\r\n") },

            };
            clientImpl.Members.AddRange(GetImplementationMembers(result));

            Context.Location.CurrentNamespace.CompilationUnits.Add(clientImpl);
        }

        private IEnumerable<RtNode> GetImplementationMembers(RtInterface result)
        {
            var members = new List<RtNode>();
            var functions = result.Members.OfType<RtFuncion>();
            foreach (var function in functions)
            {
                var rtTypeNames = function.Arguments.Select(a => a.Type.ToString()).ToList();
                var generics = string.Join(",", rtTypeNames);
                if (rtTypeNames.Count > 1)
                {
                    generics = $"[{generics}]";
                }
                var stringType = rtTypeNames.Count == 0 ? "SignalDispatcher" : $"SimpleEventDispatcher<{generics}>";
                var eventDispatcher = new RtField
                {
                    AccessModifier = AccessModifier.Public,
                    Identifier = new RtIdentifier($"on{function.Identifier.ToString().FirstCharToUpper()}"),
                    Type = new RtSimpleTypeName(stringType),
                    InitializationExpression = $"new {stringType}()"
                };
                members.Add(eventDispatcher);
            }

            members.Add(new RtConstructor
            {
                Arguments = { new RtArgument
                {
                    Type = new RtSimpleTypeName("HubConnectionProvider"),
                    Identifier = new RtIdentifier("hubConnectionProvider")
                }},
                Body = GetEventRegistrationBody(functions)
            });

            return members;
        }

        private RtRaw GetEventRegistrationBody(IEnumerable<RtFuncion> functions)
        {
            var pre = "hubConnectionProvider.getHubConnection(\"/hub\").then(hubConnection => {\r\n";
            var e = functions.Select(f =>
            {
                var args = f.Arguments.Select(a => $"{a.Identifier} : {a.Type}").ToList();
                var response = $"({string.Join(",", args)})";
                var commaArgs = string.Join(",", f.Arguments.Select(a => a.Identifier));
                if (args.Count > 1)
                {
                    commaArgs = $"[{commaArgs}]";
                }
                return $"  hubConnection.on(\"{f.Identifier}\", {response} => {{\r\n      console.log(\"{f.Identifier} received from server\", {commaArgs});\r\n      this.on{f.Identifier.ToString().FirstCharToUpper()}.dispatch({commaArgs});\r\n    }});";
            });
            var post = "\r\n});";
            return new RtRaw(pre + string.Join("\r\n", e) + post);
        }
    }
}
