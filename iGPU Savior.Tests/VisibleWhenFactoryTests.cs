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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Toggle_PreservesTargetAndExpectedValue(bool expectedValue)
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", expectedValue);
            Assert.Equal("EnableAdvanced", cond.TargetKey);
            var typed = Assert.IsType<ToggleCondition>(cond);
            Assert.Equal(expectedValue, typed.ExpectedValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DropdownOption_RejectsEmptyTargetKey(string targetKey)
        {
            Assert.Throws<ArgumentException>(() => VisibleWhen.DropdownOption(targetKey, "OpenMeteo"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DropdownIndex_RejectsEmptyTargetKey(string targetKey)
        {
            Assert.Throws<ArgumentException>(() => VisibleWhen.DropdownIndex(targetKey, 0));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Toggle_RejectsEmptyTargetKey(string targetKey)
        {
            Assert.Throws<ArgumentException>(() => VisibleWhen.Toggle(targetKey, true));
        }

        [Fact]
        public void DropdownOption_RejectsNullExpectedOption()
        {
            Assert.Throws<ArgumentNullException>(() => VisibleWhen.DropdownOption("Provider", null));
        }

        [Fact]
        public void DropdownOption_RejectsEmptyExpectedOption()
        {
            Assert.Throws<ArgumentException>(() => VisibleWhen.DropdownOption("Provider", string.Empty));
        }
    }
}
