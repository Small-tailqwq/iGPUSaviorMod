using PotatoOptimization.Patches;
using Xunit;

namespace IGPUSavior.Tests
{
    public class NoteExportStateCapturePolicyTests
    {
        [Theory]
        [InlineData(false, false, true, true)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, false, true)]
        [InlineData(true, false, true, true)]
        [InlineData(true, false, false, true)]
        [InlineData(true, true, false, true)]
        [InlineData(true, true, true, false)]
        public void ShouldCaptureOriginalState_MatchesSelectionModeRules(
            bool isSelectionMode,
            bool hasOriginalState,
            bool sameControlInstance,
            bool expected)
        {
            bool shouldCapture = NoteExportStateCapturePolicy.ShouldCaptureOriginalState(
                isSelectionMode,
                hasOriginalState,
                sameControlInstance);

            Assert.Equal(expected, shouldCapture);
        }
    }
}
