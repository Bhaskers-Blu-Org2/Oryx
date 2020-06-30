using Microsoft.Oryx.Detector;

namespace Microsoft.Oryx.Tests.Common
{
    public class TestDetector : Detector.IPlatformDetector
    {
        private readonly PlatformDetectorResult _detectorResult;

        public TestDetector(PlatformName platformName, PlatformDetectorResult detectorResult)
        {
            PlatformName = platformName;
            _detectorResult = detectorResult;
        }

        public PlatformName PlatformName { get; }

        public PlatformDetectorResult Detect(DetectorContext context)
        {
            return _detectorResult;
        }
    }
}
