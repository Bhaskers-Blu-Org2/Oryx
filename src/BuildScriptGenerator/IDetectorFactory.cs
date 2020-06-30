using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Oryx.Detector;
using Microsoft.Oryx.Detector.DotNetCore;
using Microsoft.Oryx.Detector.Hugo;
using Microsoft.Oryx.Detector.Node;
using Microsoft.Oryx.Detector.Php;
using Microsoft.Oryx.Detector.Python;

namespace Microsoft.Oryx.BuildScriptGenerator
{
    public interface IDetectorFactory
    {
        Detector.IPlatformDetector GetDetector(PlatformName platformName);
    }

    public class DefaultDetectorFactory : IDetectorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultDetectorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Detector.IPlatformDetector GetDetector(PlatformName platformName)
        {
            if (platformName == PlatformName.DotNetCore)
            {
                return _serviceProvider.GetRequiredService<DotNetCoreDetector>();
            }

            if (platformName == PlatformName.Node)
            {
                return _serviceProvider.GetRequiredService<NodeDetector>();
            }

            if (platformName == PlatformName.Hugo)
            {
                return _serviceProvider.GetRequiredService<HugoDetector>();
            }

            if (platformName == PlatformName.Php)
            {
                return _serviceProvider.GetRequiredService<PhpDetector>();
            }

            if (platformName == PlatformName.Python)
            {
                return _serviceProvider.GetRequiredService<PythonDetector>();
            }

            throw new InvalidOperationException($"Unrecognized platform name '{platformName}'.");
        }
    }
}
