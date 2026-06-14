using System;
using ModShared;
using Xunit;

namespace IGPUSavior.Tests
{
    public class VisibleWhenFactoryTests
    {
        [Fact]
        public void DropdownOption_PreservesTargetAndExpectedOption()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "OpenMeteo");
            Assert.Equal("Provider", cond.TargetKey);
            var typed = Assert.IsType<DropdownOptionCondition>(cond);
            Assert.Equal("OpenMeteo", typed.ExpectedOption);
        }

        [Fact]
        public void DropdownIndex_PreservesTargetAndExpectedIndex()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 2);
            Assert.Equal("Provider", cond.TargetKey);
            var typed = Assert.IsType<DropdownIndexCondition>(cond);
            Assert.Equal(2, typed.ExpectedIndex);
        }

        [Fact]
        public void Toggle_PreservesTargetAndExpectedValue()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", true);
            Assert.Equal("EnableAdvanced", cond.TargetKey);
            var typed = Assert.IsType<ToggleCondition>(cond);
            Assert.True(typed.ExpectedValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DropdownOption_RejectsEmptyTargetKey(string targetKey)
        {
            Assert.Throws<ArgumentException>(() => VisibleWhen.DropdownOption(targetKey, "OpenMeteo"));
        }

        [Fact]
        public void DropdownOption_RejectsNullExpectedOption()
        {
            Assert.Throws<ArgumentNullException>(() => VisibleWhen.DropdownOption("Provider", null));
        }
    }
}
