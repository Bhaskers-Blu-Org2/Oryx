﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Oryx.Common.Extensions;

namespace Microsoft.Oryx.Detector
{
    public class LocalSourceRepo : ISourceRepo
    {
        private readonly ILogger<LocalSourceRepo> _logger;

        public LocalSourceRepo(string sourceDirectory)
        {
            RootPath = sourceDirectory;
            _logger = NullLogger<LocalSourceRepo>.Instance;
        }

        public LocalSourceRepo(string sourceDirectory, ILoggerFactory loggerFactory)
        {
            RootPath = sourceDirectory;
            _logger = loggerFactory.CreateLogger<LocalSourceRepo>();
        }

        public string RootPath { get; }

        public bool FileExists(params string[] paths)
        {
            var path = ResolvePath(paths);
            return File.Exists(path);
        }

        public bool DirExists(params string[] paths)
        {
            var path = ResolvePath(paths);
            return Directory.Exists(path);
        }

        public IEnumerable<string> EnumerateFiles(string searchPattern, bool searchSubDirectories)
        {
            if (searchSubDirectories)
            {
                return RootPath.SafeEnumerateFiles(searchPattern);
            }

            return Directory.EnumerateFiles(RootPath, searchPattern);
        }

        public string ReadFile(params string[] paths)
        {
            var path = ResolvePath(paths);
            return File.ReadAllText(path);
        }

        public string[] ReadAllLines(params string[] paths)
        {
            var path = ResolvePath(paths);
            return File.ReadAllLines(path);
        }

        private string ResolvePath(params string[] paths)
        {
            var filePathInRepo = Path.Combine(paths);
            return Path.Combine(RootPath, filePathInRepo);
        }
    }
}