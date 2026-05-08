namespace PotatoOptimization.Patches
{
    internal static class NoteExportStateCapturePolicy
    {
        internal static bool ShouldCaptureOriginalState(bool isSelectionMode, bool hasOriginalState, bool sameControlInstance)
        {
            return !isSelectionMode || !hasOriginalState || !sameControlInstance;
        }
    }
}
