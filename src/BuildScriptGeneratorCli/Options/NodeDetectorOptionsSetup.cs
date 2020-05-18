﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.Node;

namespace Microsoft.Oryx.BuildScriptGeneratorCli.Options
{
    /// <summary>
    /// Gets hierarchical configuration from IConfiguration api and binds the properties on NodeDetectorOptions.
    /// </summary>
    public class NodeDetectorOptionsSetup : OptionsSetupBase, IConfigureOptions<NodeDetectorOptions>
    {
        public NodeDetectorOptionsSetup(IConfiguration configuration)
            : base(configuration)
        {
        }

        public void Configure(NodeDetectorOptions options)
        {
        }
    }
}
