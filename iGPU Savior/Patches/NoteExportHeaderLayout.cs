namespace PotatoOptimization.Patches
{
    internal static class NoteExportHeaderLayout
    {
        internal static float GetPrimaryButtonX(float closeButtonX, float closeButtonWidth, float buttonWidth, float gap)
        {
            return closeButtonX - ((closeButtonWidth + buttonWidth) * 0.5f) - gap;
        }

        internal static float GetSecondaryButtonX(float primaryButtonX, float buttonWidth, float gap)
        {
            return primaryButtonX - buttonWidth - gap;
        }

        internal static float GetButtonY(float noteTitleTextY)
        {
            return noteTitleTextY;
        }
    }
}
