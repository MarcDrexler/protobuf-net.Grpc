﻿using Grpc.Core;
using ProtoBuf.Grpc.Configuration;
using System;
using System.IO;
using static Grpc.Core.Server;

namespace ProtoBuf.Grpc.Server
{
    /// <summary>
    /// Provides extension methods to the ServiceDefinitionCollection API
    /// </summary>
    public static class ServiceDefinitionCollectionExtensions
    {
        /// <summary>
        /// Adds a code-first service to the available services
        /// </summary>
        public static int AddCodeFirst<TService>(this ServiceDefinitionCollection services, TService service,
            BinderConfiguration? binderConfiguration = null,
            TextWriter? log = null)
            where TService : class
        {
            var builder = ServerServiceDefinition.CreateBuilder();
            int count = Binder.Create(log).Bind<TService>(builder, binderConfiguration, service);
            services.Add(builder.Build());
            return count;
        }

        private class Binder : ServerBinder
        {
            private readonly TextWriter? _log;
            private Binder(TextWriter? log) => _log = log;
            private static readonly Binder _default = new Binder(null);
            public static Binder Create(TextWriter? log) => log == null ? _default : new Binder(log);

            protected override void OnServiceBound(object state, string serviceName, Type serviceType, Type serviceContract, int operationCount)
            {
                base.OnServiceBound(state, serviceName, serviceType, serviceContract, operationCount);
                _log?.WriteLine($"{serviceName} bound to {serviceType.Name} : {serviceContract.Name} with {operationCount} operation(s)");
            }
            protected override bool TryBind<TService, TRequest, TResponse>(object state, Method<TRequest, TResponse> method, MethodStub<TService> stub)
                where TService : class
                where TRequest : class
                where TResponse : class
            {
                var builder = (ServerServiceDefinition.Builder)state;
                switch (method.Type)
                {
                    case MethodType.Unary:
                        builder.AddMethod(method, stub.CreateDelegate<UnaryServerMethod<TRequest, TResponse>>());
                        break;
                    case MethodType.ClientStreaming:
                        builder.AddMethod(method, stub.CreateDelegate<ClientStreamingServerMethod<TRequest, TResponse>>());
                        break;
                    case MethodType.ServerStreaming:
                        builder.AddMethod(method, stub.CreateDelegate<ServerStreamingServerMethod<TRequest, TResponse>>());
                        break;
                    case MethodType.DuplexStreaming:
                        builder.AddMethod(method, stub.CreateDelegate<DuplexStreamingServerMethod<TRequest, TResponse>>());
                        break;
                    default:
                        return false;
                }
                _log?.WriteLine($"{method.ServiceName} / {method.Name} ({method.Type}) bound to {stub.Method.DeclaringType.Name}.{stub.Method.Name}");
                return true;
            }
        }
    }
}
