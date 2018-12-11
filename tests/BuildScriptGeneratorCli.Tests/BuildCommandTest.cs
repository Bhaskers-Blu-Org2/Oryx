﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.BuildScriptGeneratorCli;
using Oryx.Tests.Common;
using Xunit;

namespace BuildScriptGeneratorCli.Tests
{
    public class BuildCommandTest : IClassFixture<TestTempDirTestFixture>
    {
        private static string _testDirPath;

        public BuildCommandTest(TestTempDirTestFixture testFixture)
        {
            _testDirPath = testFixture.RootDirPath;
        }

        [Fact]
        public void OnExecute_ShowsHelp_AndExits_WhenSourceDirectoryDoesNotExist()
        {
            // Arrange
            var buildCommand = new BuildCommand
            {
                SourceDir = CreatePathForNewDir(),
                DestinationDir = CreatePathForNewDir(),
            };
            var testConsole = new TestConsole();

            // Act
            var exitCode = buildCommand.OnExecute(new CommandLineApplication(testConsole), testConsole);

            // Assert
            Assert.NotEqual(0, exitCode);
            var error = testConsole.StdError;
            Assert.DoesNotContain("Usage:", error);
            Assert.Contains("Could not find the source directory", error);
        }

        [Fact]
        public void Configure_UsesCurrentDirectory_WhenSourceDirectoryNotSupplied()
        {
            // Arrange
            var scriptCommand = new BuildCommand { SourceDir = string.Empty };
            var testConsole = new TestConsole();

            // Act
            BuildScriptGeneratorOptions opts = new BuildScriptGeneratorOptions();
            scriptCommand.ConfigureBuildScriptGeneratorOptions(opts);

            // Assert
            Assert.Equal(Directory.GetCurrentDirectory(), opts.SourceDir);
        }

        [Fact]
        public void IsValidInput_IsTrue_EvenIfDestinationDirDoesNotExist()
        {
            // Arrange
            var serviceProvider = new ServiceProviderBuilder()
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = CreateNewDir();
                    o.DestinationDir = CreatePathForNewDir();
                })
                .Build();
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand();


            // Act
            var isValid = buildCommand.IsValidInput(serviceProvider, testConsole);

            // Assert
            Assert.True(isValid);
            Assert.Empty(testConsole.StdOutput);
            Assert.Empty(testConsole.StdError);
        }

        [Fact]
        public void IsValidInput_IsTrue_EvenIfDestinationDirExists_AndIsEmpty()
        {
            // Arrange
            var serviceProvider = new ServiceProviderBuilder()
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = CreateNewDir();
                    o.DestinationDir = CreateNewDir();
                })
                .Build();
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand();

            // Act
            var isValid = buildCommand.IsValidInput(serviceProvider, testConsole);

            // Assert
            Assert.True(isValid);
            Assert.Empty(testConsole.StdOutput);
            Assert.Empty(testConsole.StdError);
        }

        public static TheoryData<string, string> IsSubDirectoryTrueData
        {
            get
            {
                var data = new TheoryData<string, string>
                {
                    {
                        Path.Combine("c:", "foo"),
                        Path.Combine("c:", "foo")
                    },
                    {
                        Path.Combine("c:", "foo") + Path.DirectorySeparatorChar,
                        Path.Combine("c:", "foo")
                    },
                    {
                        Path.Combine("c:", "foo"),
                        Path.Combine("c:", "foo") + Path.DirectorySeparatorChar
                    },
                    {
                        Path.Combine("c:", "foo") + Path.DirectorySeparatorChar,
                        Path.Combine("c:", "foo") + Path.DirectorySeparatorChar
                    },
                    {
                        Path.Combine("c:", "foo", "bar"),
                        Path.Combine("c:", "foo")
                    },
                    {
                        Path.Combine("c:", "foo", "bar", "dir1", "dir2"),
                        Path.Combine("c:", "foo")
                    },
                    {
                        Path.Combine(Path.DirectorySeparatorChar.ToString(), "foo"),
                        Path.DirectorySeparatorChar.ToString()
                    },
                    {
                        Path.GetFullPath(Path.Combine("a", "b", "c", "d", "..")),
                        Path.GetFullPath(Path.Combine("a", "b"))
                    },
                };

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsSubDirectoryTrueData))]
        public void IsSubDirectory_IsTrue(string dir1, string dir2)
        {
            // Arrange
            var buildCommand = new BuildCommand();

            // Act
            var isSubDirectory = buildCommand.IsSubDirectory(dir1, dir2);

            // Assert
            Assert.True(isSubDirectory);
        }

        public static TheoryData<string, string> IsSubDirectoryFalseData
        {
            get
            {
                var data = new TheoryData<string, string>
                {
                    {
                        // case-sensitive
                        Path.Combine("c:", "Foo"),
                        Path.Combine("c:", "foo")
                    },
                    {
                        Path.Combine("c:", "foo"),
                        Path.Combine("c:", "foo", "bar")
                    },
                    {
                        Path.Combine("a", "b", "c"),
                        Path.Combine("a", "b", "cd")
                    },
                    {
                        Path.DirectorySeparatorChar.ToString(),
                        Path.Combine(Path.DirectorySeparatorChar.ToString(), "foo")
                    },
                    {
                        Path.GetFullPath(Path.Combine("a", "b", "c", "..", "..")),
                        Path.GetFullPath(Path.Combine("a", "b"))
                    },
                };

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IsSubDirectoryFalseData))]
        public void IsSubDirectory_IsFalse(string dir1, string dir2)
        {
            // Arrange
            var buildCommand = new BuildCommand();

            // Act
            var isSubDirectory = buildCommand.IsSubDirectory(dir1, dir2);

            // Assert
            Assert.False(isSubDirectory);
        }

        public static TheoryData<string> DestinationDirectoryPathData
        {
            get
            {
                var data = new TheoryData<string>();

                // Sub-directory with a file
                var destinationDir = Directory.CreateDirectory(
                    Path.Combine(_testDirPath, Guid.NewGuid().ToString()));
                var subDir = Directory.CreateDirectory(
                    Path.Combine(destinationDir.FullName, Guid.NewGuid().ToString()));
                File.WriteAllText(Path.Combine(subDir.FullName, "file1.txt"), "file1 content");
                data.Add(destinationDir.FullName);

                // Sub-directory which is empty
                destinationDir = Directory.CreateDirectory(
                    Path.Combine(_testDirPath, Guid.NewGuid().ToString()));
                subDir = Directory.CreateDirectory(
                    Path.Combine(destinationDir.FullName, Guid.NewGuid().ToString()));
                data.Add(destinationDir.FullName);

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(DestinationDirectoryPathData))]
        public void IsValidInput_IsTrue_IfDestinationDirIsNotEmpty(
            string destinationDir)
        {
            // Arrange
            var serviceProvider = new ServiceProviderBuilder()
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = CreateNewDir();
                    o.DestinationDir = destinationDir;
                })
                .Build();
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand();

            // Act
            var isValid = buildCommand.IsValidInput(serviceProvider, testConsole);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void DoesNotShowHelp_EvenIfIntermediateDir_DoesNotExistYet()
        {
            // Arrange
            var buildCommand = new CustomBuildCommand
            {
                SourceDir = CreateNewDir(),
                DestinationDir = CreateNewDir(),
                // New directory which does not exist yet
                IntermediateDir = CreatePathForNewDir()
            };
            var testConsole = new TestConsole();

            // Act
            var exitCode = buildCommand.OnExecute(new CommandLineApplication(testConsole), testConsole);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Empty(testConsole.StdOutput);
            Assert.Empty(testConsole.StdError);
        }

        // We want to test that only build output is visible on standard output stream when a build happens
        // successfully. But for this we cannot rely on the built-in generators as their content could change
        // making this test unreliable. So we use a test generator which always outputs content that we know for
        // sure wouldn't change. Since we cannot update product code with test generator we cannot run this test in
        // a docker container. So we run this test on a Linux OS only as build sets execute permission flag and
        // as well as executes a bash script.
        [EnableOnPlatform("LINUX")]
        public void OnSuccess_Execute_WritesOnlyBuildOutput_ToStandardOutput()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider(new TestScriptGenerator(), scriptOnly: false);
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var buildCommand = new BuildCommand();
            var testConsole = new TestConsole(newLineCharacter: string.Empty);

            // Act
            var exitCode = buildCommand.Execute(
                serviceProvider,
                testConsole,
                stdOutHandler: (sender, args) =>
                {
                    outputBuilder.AppendLine(args.Data);
                },
                stdErrHandler: (sender, args) =>
                {
                    errorBuilder.AppendLine(args.Data);
                });

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, testConsole.StdError);
            Assert.Equal("Hello World", outputBuilder.ToString().Replace(Environment.NewLine, string.Empty));
            Assert.Equal(string.Empty, errorBuilder.ToString().Replace(Environment.NewLine, string.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData("subdir1")]
        [InlineData("subdir1", "subdir2")]
        public void IsValid_IsFalse_IfDestinationDir_IsSubDirectory_OfSourceDir_AndIntermediateDirIsProvided(params string[] paths)
        {
            // Arrange
            var sourceDir = CreateNewDir();
            var intDir = CreateNewDir();
            var subPaths = Path.Combine(paths);
            var destinationDir = Path.Combine(sourceDir, subPaths);
            var serviceProvider = new ServiceProviderBuilder()
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = sourceDir;
                    o.IntermediateDir = intDir;
                    o.DestinationDir = destinationDir;
                })
                .Build();
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand();

            // Act
            var isValid = buildCommand.IsValidInput(serviceProvider, testConsole);

            // Assert
            Assert.False(isValid);
            Assert.Contains(
                $"Destination directory '{destinationDir}' cannot be a " +
                $"sub-directory of source directory '{sourceDir}'.",
                testConsole.StdError);
        }

        [Theory]
        [InlineData("")]
        [InlineData("subdir1")]
        [InlineData("subdir1", "subdir2")]
        public void IsValid_IsFalse_IfIntermediateDir_IsSubDirectory_OfSourceDir(params string[] paths)
        {
            // Arrange
            var sourceDir = CreateNewDir();
            var subPaths = Path.Combine(paths);
            var intermediateDir = Path.Combine(sourceDir, subPaths);
            var serviceProvider = new ServiceProviderBuilder()
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = sourceDir;
                    o.IntermediateDir = intermediateDir;
                    o.DestinationDir = CreateNewDir();
                })
                .Build();
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand();

            // Act
            var isValid = buildCommand.IsValidInput(serviceProvider, testConsole);

            // Assert
            Assert.False(isValid);
            Assert.Contains(
                $"Intermediate directory '{intermediateDir}' cannot be a " +
                $"sub-directory of source directory '{sourceDir}'.",
                testConsole.StdError);
        }

        [Fact]
        public void IsValid_IsFalse_IfLanguageVersionSpecified_WithoutLanguageName()
        {
            // Arrange
            var serviceProvider = new ServiceProviderBuilder()
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = CreateNewDir();
                    o.DestinationDir = CreateNewDir();
                    o.Language = null;
                    o.LanguageVersion = "1.0.0";
                })
                .Build();
            var testConsole = new TestConsole();
            var buildCommand = new BuildCommand();

            // Act
            var isValid = buildCommand.IsValidInput(serviceProvider, testConsole);

            // Assert
            Assert.False(isValid);
            Assert.Contains(
                "Cannot use language version without specifying language name also.",
                testConsole.StdError);
        }

        private string CreateNewDir()
        {
            return Directory.CreateDirectory(CreatePathForNewDir()).FullName;
        }

        private string CreatePathForNewDir()
        {
            return Path.Combine(_testDirPath, Guid.NewGuid().ToString());
        }

        private IServiceProvider CreateServiceProvider(TestScriptGenerator generator, bool scriptOnly)
        {
            var sourceCodeFolder = Path.Combine(_testDirPath, "src");
            Directory.CreateDirectory(sourceCodeFolder);
            var outputFolder = Path.Combine(_testDirPath, "output");
            Directory.CreateDirectory(outputFolder);
            var servicesBuilder = new ServiceProviderBuilder()
                .ConfigureServices(services =>
                {
                    // Add 'test' script generator here as we can control what the script output is rather
                    // than depending on in-built script generators whose script could change overtime causing
                    // this test to be difficult to manage.
                    services.RemoveAll<ILanguageDetector>();
                    services.TryAddEnumerable(
                        ServiceDescriptor.Singleton<ILanguageDetector, TestLanguageDetector>());
                    services.RemoveAll<ILanguageScriptGenerator>();
                    services.TryAddEnumerable(
                        ServiceDescriptor.Singleton<ILanguageScriptGenerator>(generator));
                    services.AddSingleton<ITempDirectoryProvider>(
                        new TestTempDirectoryProvider(Path.Combine(_testDirPath, "temp")));
                })
                .ConfigureScriptGenerationOptions(o =>
                {
                    o.SourceDir = sourceCodeFolder;
                    o.DestinationDir = outputFolder;
                    o.ScriptOnly = scriptOnly;
                });
            return servicesBuilder.Build();
        }

        private class CustomBuildCommand : BuildCommand
        {
            internal override int Execute(IServiceProvider serviceProvider, IConsole console)
            {
                return 0;
                //return base.Execute(serviceProvider, console);
            }
        }

        private class TestTempDirectoryProvider : ITempDirectoryProvider
        {
            private readonly string _tempDir;

            public TestTempDirectoryProvider(string tempDir)
            {
                _tempDir = tempDir;
            }

            public string GetTempDirectory()
            {
                Directory.CreateDirectory(_tempDir);
                return _tempDir;
            }
        }

        private class TestScriptGenerator : ILanguageScriptGenerator
        {
            private readonly string _scriptContent;

            public TestScriptGenerator()
                : this(scriptContent: null)
            {
            }

            public TestScriptGenerator(string scriptContent)
            {
                _scriptContent = scriptContent;
            }

            public string SupportedLanguageName => "test";

            public IEnumerable<string> SupportedLanguageVersions => new[] { "1.0.0" };

            public bool TryGenerateBashScript(ScriptGeneratorContext scriptGeneratorContext, out string script)
            {
                if (string.IsNullOrEmpty(_scriptContent))
                {
                    script = "#!/bin/bash" + Environment.NewLine + "echo Hello World" + Environment.NewLine;
                }
                else
                {
                    script = _scriptContent;
                }

                return true;
            }
        }

        private class TestLanguageDetector : ILanguageDetector
        {
            public LanguageDetectorResult Detect(ISourceRepo sourceRepo)
            {
                return new LanguageDetectorResult
                {
                    Language = "test",
                    LanguageVersion = "1.0.0"
                };
            }
        }

        private class TestScriptExecutor : IScriptExecutor
        {
            public string ScriptPath { get; private set; }
            public string[] Args { get; private set; }
            public bool ExecuteScriptCalled { get; private set; }
            public int ReturnExitCode { get; }

            public TestScriptExecutor(int returnExitCode)
            {
                ReturnExitCode = returnExitCode;
            }

            public int ExecuteScript(
                string scriptPath,
                string[] args,
                string workingDirectory,
                DataReceivedEventHandler stdOutHandler,
                DataReceivedEventHandler stdErrHandler)
            {
                ScriptPath = scriptPath;
                Args = args;
                ExecuteScriptCalled = true;
                return ReturnExitCode;
            }
        }
    }
}
