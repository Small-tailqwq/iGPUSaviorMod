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

        internal static float GetStackedControlCenterY(
            float upperControlCenterY,
            float upperControlHeight,
            float lowerControlHeight,
            float gap)
        {
            return upperControlCenterY - ((upperControlHeight + lowerControlHeight) * 0.5f) - gap;
        }

        internal static float GetFollowingContentTopY(float controlCenterY, float controlHeight, float gap)
        {
            return controlCenterY - (controlHeight * 0.5f) - gap;
        }

        internal static float GetFollowingControlCenterX(
            float leftControlCenterX,
            float leftControlWidth,
            float rightControlWidth,
            float gap)
        {
            return leftControlCenterX + ((leftControlWidth + rightControlWidth) * 0.5f) + gap;
        }
    }
}
