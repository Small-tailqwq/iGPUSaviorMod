using PotatoOptimization.Patches;
using Xunit;

namespace IGPUSavior.Tests
{
    public class NoteExportHeaderLayoutTests
    {
        [Fact]
        public void GetPrimaryButtonX_UsesCloseButtonSpacing()
        {
            float buttonX = NoteExportHeaderLayout.GetPrimaryButtonX(-24f, 28f, 160f, 6f);

            Assert.Equal(-124f, buttonX, 3);
        }

        [Fact]
        public void GetSecondaryButtonX_PlacesConfirmToLeftOfPrimary()
        {
            float buttonX = NoteExportHeaderLayout.GetSecondaryButtonX(-124f, 160f, 6f);

            Assert.Equal(-290f, buttonX, 3);
        }

        [Fact]
        public void GetButtonY_UsesTitleTextRow()
        {
            float buttonY = NoteExportHeaderLayout.GetButtonY(-46.8f);

            Assert.Equal(-46.8f, buttonY, 3);
        }
    }
}
