using PotatoOptimization.UI;
using Xunit;

public class ModSettingsRectLayoutSnapshotTests
{
  [Fact]
  public void WithHorizontalFromCopiesNativeHorizontalValuesAndKeepsTargetVerticalValues()
  {
    var native = new ModSettingsRectLayoutSnapshot(
      anchorMinX: 0f,
      anchorMinY: 1f,
      anchorMaxX: 1f,
      anchorMaxY: 0f,
      pivotX: 1f,
      pivotY: 1f,
      offsetMinX: -114f,
      offsetMinY: -418.4f,
      offsetMaxX: 0f,
      offsetMaxY: 590.1f);

    var target = new ModSettingsRectLayoutSnapshot(
      anchorMinX: 0f,
      anchorMinY: 1f,
      anchorMaxX: 1f,
      anchorMaxY: 0f,
      pivotX: 1f,
      pivotY: 1f,
      offsetMinX: -125.5f,
      offsetMinY: -526.1f,
      offsetMaxX: 0f,
      offsetMaxY: 1322.2f);

    var result = target.WithHorizontalFrom(native);

    Assert.Equal(native.AnchorMinX, result.AnchorMinX);
    Assert.Equal(native.AnchorMaxX, result.AnchorMaxX);
    Assert.Equal(native.PivotX, result.PivotX);
    Assert.Equal(native.OffsetMinX, result.OffsetMinX);
    Assert.Equal(native.OffsetMaxX, result.OffsetMaxX);

    Assert.Equal(target.AnchorMinY, result.AnchorMinY);
    Assert.Equal(target.AnchorMaxY, result.AnchorMaxY);
    Assert.Equal(target.PivotY, result.PivotY);
    Assert.Equal(target.OffsetMinY, result.OffsetMinY);
    Assert.Equal(target.OffsetMaxY, result.OffsetMaxY);
  }
}
