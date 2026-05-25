using System.Linq;
using System.Reflection;
using PotatoOptimization.UI;
using Xunit;

namespace IGPUSavior.Tests
{
    public class SettingUITabFieldSelectorTests
    {
        [Fact]
        public void GetTabInteractableFields_ExcludesInteractablesWithoutMatchingParentField()
        {
            var result = SettingUITabFieldSelector
                .GetTabInteractableFields(
                    typeof(FakeSettingUI).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                    typeof(FakeInteractableUI),
                    typeof(FakeParent))
                .Select(field => field.Name)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    "_audioInteractableUI",
                    "_generalInteractableUI",
                    "_graphicsInteractableUI"
                },
                result.OrderBy(name => name).ToArray());
        }

        private sealed class FakeSettingUI
        {
            public FakeInteractableUI _generalInteractableUI = new FakeInteractableUI();
            public FakeInteractableUI _graphicsInteractableUI = new FakeInteractableUI();
            public FakeInteractableUI _audioInteractableUI = new FakeInteractableUI();
            public FakeInteractableUI _vSyncInteractableUI = new FakeInteractableUI();

            public FakeParent _generalParent = new FakeParent();
            public FakeParent _graphicsParent = new FakeParent();
            public FakeParent _audioParent = new FakeParent();
            public FakeParent _vSyncParentWrongSuffix = new FakeParent();
        }

        private sealed class FakeInteractableUI
        {
        }

        private sealed class FakeParent
        {
        }
    }
}
