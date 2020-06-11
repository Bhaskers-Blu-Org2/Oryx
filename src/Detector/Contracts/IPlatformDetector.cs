﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------
using Microsoft.Oryx.Common;

namespace Microsoft.Oryx.Detector
{
    /// <summary>
    /// Detects language name and version of the application in source directory.
    /// </summary>
    public interface IPlatformDetector
    {

        /// <summary>
        /// Detects language name and version of the application in source directory.
        /// </summary>
        /// <param name="context">The <see cref="RepositoryContext"/>.</param>
        /// <returns>An instance of <see cref="PlatformDetectorResult"/> if detection was
        /// successful, <c>null</c> otherwise</returns>
        PlatformDetectorResult Detect(RepositoryContext context);

        PlatformName DetectorPlatformName { get; }
    }
}