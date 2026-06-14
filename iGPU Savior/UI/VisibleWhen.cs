using System;

namespace ModShared
{
  /// <summary>
  /// Base class for visibility conditions used with <see cref="ModSettingsManager"/>.
  /// External mods should create instances through <see cref="VisibleWhen"/> factories.
  /// </summary>
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

  /// <summary>
  /// Factory methods for creating <see cref="VisibleWhenCondition"/> instances.
  /// </summary>
  public static class VisibleWhen
  {
    /// <summary>
    /// Visible when the dropdown setting identified by <paramref name="targetKey"/> is set to <paramref name="expectedOption"/>.
    /// </summary>
    public static VisibleWhenCondition DropdownOption(string targetKey, string expectedOption)
    {
      if (expectedOption == null)
        throw new ArgumentNullException(nameof(expectedOption));
      if (expectedOption == string.Empty)
        throw new ArgumentException("expectedOption cannot be empty", nameof(expectedOption));
      return new DropdownOptionCondition(targetKey, expectedOption);
    }

    /// <summary>
    /// Visible when the dropdown setting identified by <paramref name="targetKey"/> is set to <paramref name="expectedIndex"/>.
    /// </summary>
    public static VisibleWhenCondition DropdownIndex(string targetKey, int expectedIndex)
      => new DropdownIndexCondition(targetKey, expectedIndex);

    /// <summary>
    /// Visible when the toggle setting identified by <paramref name="targetKey"/> equals <paramref name="expectedValue"/>.
    /// </summary>
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
