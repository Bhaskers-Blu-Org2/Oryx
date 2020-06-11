﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Oryx.BuildScriptGenerator.DotNetCore;
using Microsoft.Oryx.BuildScriptGenerator.Node;
using Microsoft.Oryx.BuildScriptGenerator.Php;
using Microsoft.Oryx.BuildScriptGenerator.Python;
using Microsoft.Oryx.Common;
using Microsoft.Oryx.Detector;
using Polly;
using Polly.Extensions.Http;

namespace Microsoft.Oryx.BuildScriptGenerator
{
    public static class BuildScriptGeneratorServiceCollectionExtensions
    {
        public static IServiceCollection AddBuildScriptGeneratorServices(this IServiceCollection services)
        {
            services
                .AddPlatformDetectorServices()
                .AddNodeScriptGeneratorServices()
                .AddHugoScriptGeneratorServices()
                .AddPythonScriptGeneratorServices()
                .AddDotNetCoreScriptGeneratorServices()
                .AddPhpScriptGeneratorServices();

            services.AddSingleton<IBuildScriptGenerator, DefaultBuildScriptGenerator>();
            services.AddSingleton<ICompatiblePlatformDetector, DefaultCompatiblePlatformDetector>();
            services.AddSingleton<IDockerfileGenerator, DefaultDockerfileGenerator>();
            services.AddSingleton<IEnvironment, DefaultEnvironment>();
            services.AddSingleton<ISourceRepoProvider, DefaultSourceRepoProvider>();
            services.AddSingleton<ITempDirectoryProvider, DefaulTempDirectoryProvider>();
            services.AddSingleton<IScriptExecutor, DefaultScriptExecutor>();
            services.AddSingleton<IRunScriptGenerator, DefaultRunScriptGenerator>();
            services.TryAddEnumerable(
               ServiceDescriptor.Singleton<IPlatformDetector, NodePlatformDetector>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPlatformDetector, DotNetCorePlatformDetector>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPlatformDetector, PythonPlatformDetector>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPlatformDetector, PhpPlatformDetector>());
            services.AddHttpClient("general", httpClient =>
            {
                // NOTE: Setting user agent is required to avoid receiving 403 Forbidden response.
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("oryx", "1.0"));
            }).AddPolicyHandler(GetRetryPolicy());

            // Add all checkers (platform-dependent + platform-independent)
            foreach (Type type in typeof(BuildScriptGeneratorServiceCollectionExtensions).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(CheckerAttribute), false).Length > 0)
                {
                    services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IChecker), type));
                }
            }

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(
                    retryCount: 6,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}