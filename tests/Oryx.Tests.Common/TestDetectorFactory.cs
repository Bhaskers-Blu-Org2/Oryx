using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.Detector;

namespace Microsoft.Oryx.Tests.Common
{
    public class TestDetectorFactory : IDetectorFactory
    {
        private readonly Detector.IPlatformDetector _detector;

        public TestDetectorFactory(Detector.IPlatformDetector detector)
        {
            _detector = detector;
        }

        public Detector.IPlatformDetector GetDetector(PlatformName platformName)
        {
            return _detector;
        }
    }
}
