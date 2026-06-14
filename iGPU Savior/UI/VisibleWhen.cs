using System;

namespace ModShared
{
    public abstract class VisibleWhenCondition
    {
        public string TargetKey { get; }

        internal VisibleWhenCondition(string targetKey)
        {
            if (string.IsNullOrEmpty(targetKey))
                throw new ArgumentException("targetKey cannot be null or empty", nameof(targetKey));
            TargetKey = targetKey;
        }
    }

    public static class VisibleWhen
    {
        public static VisibleWhenCondition DropdownOption(string targetKey, string expectedOption)
        {
            if (expectedOption == null)
                throw new ArgumentNullException(nameof(expectedOption));
            return new DropdownOptionCondition(targetKey, expectedOption);
        }

        public static VisibleWhenCondition DropdownIndex(string targetKey, int expectedIndex)
            => new DropdownIndexCondition(targetKey, expectedIndex);

        public static VisibleWhenCondition Toggle(string targetKey, bool expectedValue)
            => new ToggleCondition(targetKey, expectedValue);
    }

    internal sealed class DropdownOptionCondition : VisibleWhenCondition
    {
        public string ExpectedOption { get; }
        internal DropdownOptionCondition(string targetKey, string expectedOption) : base(targetKey)
            => ExpectedOption = expectedOption;
    }

    internal sealed class DropdownIndexCondition : VisibleWhenCondition
    {
        public int ExpectedIndex { get; }
        internal DropdownIndexCondition(string targetKey, int expectedIndex) : base(targetKey)
            => ExpectedIndex = expectedIndex;
    }

    internal sealed class ToggleCondition : VisibleWhenCondition
    {
        public bool ExpectedValue { get; }
        internal ToggleCondition(string targetKey, bool expectedValue) : base(targetKey)
            => ExpectedValue = expectedValue;
    }
}
