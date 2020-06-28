// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Oryx.BuildScriptGenerator.Common;

namespace Microsoft.Oryx.BuildScriptGenerator
{
    public abstract class PlatformInstallerBase
    {
        protected readonly BuildScriptGeneratorOptions _commonOptions;
        protected readonly ILogger _logger;

        public PlatformInstallerBase(
            IOptions<BuildScriptGeneratorOptions> commonOptions,
            ILoggerFactory loggerFactory)
        {
            _commonOptions = commonOptions.Value;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        protected string GetInstallerScriptSnippet(
            string platformName,
            string version,
            string directoryToInstall = null)
        {
            var sdkStorageBaseUrl = GetPlatformBinariesStorageBaseUrl();

            var versionDirInTemp = directoryToInstall;
            if (string.IsNullOrEmpty(versionDirInTemp))
            {
                versionDirInTemp = $"{Constants.TemporaryInstallationDirectoryRoot}/{platformName}/{version}";
            }

            var snippet = new StringBuilder();
            snippet.AppendLine(
                $"/opt/oryx/installPlatform " +
                $"-p {platformName} " +
                $"-v {version} " +
                $"-d {versionDirInTemp} " +
                $"-u {sdkStorageBaseUrl}");
            return snippet.ToString();
        }

        protected bool IsVersionInstalled(string lookupVersion, string builtInDir, string dynamicInstallDir)
        {
            var versionsFromDisk = VersionProviderHelper.GetVersionsFromDirectory(builtInDir);
            if (HasVersion(versionsFromDisk))
            {
                _logger.LogDebug(
                    "Version {version} is already installed at directory {installationDir}",
                    lookupVersion,
                    builtInDir);

                return true;
            }

            versionsFromDisk = VersionProviderHelper.GetVersionsFromDirectory(dynamicInstallDir);
            if (HasVersion(versionsFromDisk))
            {
                // Only if there is a sentinel file we want to indicate that a version exists.
                // This is because a user could kill a build midway which might leave the download of an SDK
                // in a corrupt state.
                var sentinelFile = Path.Combine(
                    dynamicInstallDir,
                    lookupVersion,
                    SdkStorageConstants.SdkDownloadSentinelFileName);

                if (File.Exists(sentinelFile))
                {
                    _logger.LogDebug(
                        "Version {version} is already installed at directory {installationDir}",
                        lookupVersion,
                        dynamicInstallDir);

                    return true;
                }

                _logger.LogDebug(
                    "Directory for version {version} was already found at directory {installationDir}, " +
                    "but sentinel file {sentinelFile} was not found.",
                    lookupVersion,
                    dynamicInstallDir,
                    SdkStorageConstants.SdkDownloadSentinelFileName);
            }

            _logger.LogDebug(
                "Version {version} was not found to be installed at {builtInDir} or {dynamicInstallDir}",
                lookupVersion,
                builtInDir,
                dynamicInstallDir);

            return false;

            bool HasVersion(IEnumerable<string> versionsOnDisk)
            {
                return versionsOnDisk.Any(onDiskVersion
                    => string.Equals(lookupVersion, onDiskVersion, StringComparison.OrdinalIgnoreCase));
            }
        }

        private string GetPlatformBinariesStorageBaseUrl()
        {
            var platformBinariesStorageBaseUrl = _commonOptions.OryxSdkStorageBaseUrl;
            if (string.IsNullOrEmpty(platformBinariesStorageBaseUrl))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{SdkStorageConstants.SdkStorageBaseUrlKeyName}' is required.");
            }

            platformBinariesStorageBaseUrl = platformBinariesStorageBaseUrl.TrimEnd('/');
            return platformBinariesStorageBaseUrl;
        }
    }
}
