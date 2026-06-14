using System;
using System.Collections.Generic;
using ModShared;
using Xunit;

namespace IGPUSavior.Tests
{
    public class VisibleWhenEvaluatorTests
    {
        [Fact]
        public void DropdownOption_MatchingOption_ReturnsTrue()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "OpenMeteo");
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 1);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void DropdownOption_NonMatchingOption_ReturnsFalse()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "Seniverse");
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 1);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.False(result);
            Assert.Null(reason);
        }

        [Fact]
        public void DropdownIndex_MatchingIndex_ReturnsTrue()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 0);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void Toggle_MatchingValue_ReturnsTrue()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", true);
            var snapshot = Snapshot(SettingValueKind.Toggle, toggleValue: true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void MissingController_FailsOpenWithReason()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, null, out string reason);

            Assert.True(result);
            Assert.Equal("Controller snapshot is null", reason);
        }

        [Fact]
        public void TypeMismatch_FailsOpenWithReason()
        {
            var cond = VisibleWhen.Toggle("Provider", true);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "A", "B" },
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("Expected toggle controller", reason);
        }

        [Fact]
        public void DropdownEmptyOptions_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "A");
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: Array.Empty<string>(),
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("empty", reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DropdownIndexOutOfRange_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 5);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "A", "B" },
                index: 5);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("out of range", reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NullCondition_ReturnsTrue()
        {
            bool result = VisibleWhenEvaluator.Evaluate(null, Snapshot(SettingValueKind.Toggle, toggleValue: true), out string reason);
            Assert.True(result);
            Assert.Null(reason);
        }

        [Fact]
        public void DropdownOption_WithToggleController_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownOption("EnableAdvanced", "A");
            var snapshot = Snapshot(SettingValueKind.Toggle, toggleValue: true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("Expected dropdown controller", reason);
        }

        [Fact]
        public void DropdownIndex_WithToggleController_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownIndex("EnableAdvanced", 0);
            var snapshot = Snapshot(SettingValueKind.Toggle, toggleValue: true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("Expected dropdown controller", reason);
        }

        [Fact]
        public void DropdownOption_NullOptions_FailsOpenWithReason()
        {
            var cond = VisibleWhen.DropdownOption("Provider", "A");
            var snapshot = Snapshot(SettingValueKind.Dropdown, options: null, index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.True(result);
            Assert.Contains("empty", reason, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DropdownIndex_NonMatchingIndex_ReturnsFalse()
        {
            var cond = VisibleWhen.DropdownIndex("Provider", 1);
            var snapshot = Snapshot(
                SettingValueKind.Dropdown,
                options: new[] { "Seniverse", "OpenMeteo" },
                index: 0);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.False(result);
            Assert.Null(reason);
        }

        [Fact]
        public void Toggle_NonMatchingValue_ReturnsFalse()
        {
            var cond = VisibleWhen.Toggle("EnableAdvanced", false);
            var snapshot = Snapshot(SettingValueKind.Toggle, toggleValue: true);

            bool result = VisibleWhenEvaluator.Evaluate(cond, snapshot, out string reason);

            Assert.False(result);
            Assert.Null(reason);
        }

        private static SettingValueSnapshot Snapshot(
            SettingValueKind kind,
            IReadOnlyList<string> options = null,
            int index = 0,
            bool toggleValue = false)
        {
            return new SettingValueSnapshot
            {
                Kind = kind,
                DropdownOptions = options,
                DropdownIndex = index,
                ToggleValue = toggleValue
            };
        }
    }
}
