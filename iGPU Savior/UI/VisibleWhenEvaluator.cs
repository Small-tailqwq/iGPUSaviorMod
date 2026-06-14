using System;
using System.Collections.Generic;

namespace ModShared
{
    internal enum SettingValueKind
    {
        Toggle,
        Dropdown,
        InputField
    }

    internal sealed class SettingValueSnapshot
    {
        public SettingValueKind Kind;
        public bool ToggleValue;
        public int DropdownIndex;
        public IReadOnlyList<string> DropdownOptions;
        public string InputValue { get; set; }
    }

    internal static class VisibleWhenEvaluator
    {
        internal static bool Evaluate(
            VisibleWhenCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            failureReason = null;
            if (condition == null)
                return true;

            if (controller == null)
            {
                failureReason = "Controller snapshot is null";
                return true;
            }

            switch (condition)
            {
                case DropdownOptionCondition doc:
                    return EvaluateDropdownOption(doc, controller, out failureReason);
                case DropdownIndexCondition dic:
                    return EvaluateDropdownIndex(dic, controller, out failureReason);
                case ToggleCondition tc:
                    return EvaluateToggle(tc, controller, out failureReason);
                default:
                    failureReason = $"Unsupported condition type {condition.GetType().Name}";
                    return true;
            }
        }

        private static bool EvaluateDropdownOption(
            DropdownOptionCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            if (!ValidateDropdownController(condition.TargetKey, controller, out failureReason))
                return true;

            return string.Equals(
                controller.DropdownOptions[controller.DropdownIndex],
                condition.ExpectedOption,
                StringComparison.Ordinal);
        }

        private static bool EvaluateDropdownIndex(
            DropdownIndexCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            if (!ValidateDropdownController(condition.TargetKey, controller, out failureReason))
                return true;

            return controller.DropdownIndex == condition.ExpectedIndex;
        }

        private static bool EvaluateToggle(
            ToggleCondition condition,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            failureReason = null;
            if (controller.Kind != SettingValueKind.Toggle)
            {
                failureReason = $"Expected toggle controller for key '{condition.TargetKey}', got {controller.Kind}";
                return true;
            }

            return controller.ToggleValue == condition.ExpectedValue;
        }

        private static bool ValidateDropdownController(
            string targetKey,
            SettingValueSnapshot controller,
            out string failureReason)
        {
            failureReason = null;
            if (controller.Kind != SettingValueKind.Dropdown)
            {
                failureReason = $"Expected dropdown controller for key '{targetKey}', got {controller.Kind}";
                return false;
            }

            if (controller.DropdownOptions == null || controller.DropdownOptions.Count == 0)
            {
                failureReason = "Dropdown options are empty";
                return false;
            }

            if (controller.DropdownIndex < 0 || controller.DropdownIndex >= controller.DropdownOptions.Count)
            {
                failureReason = $"Dropdown index {controller.DropdownIndex} is out of range";
                return false;
            }

            return true;
        }
    }
}
