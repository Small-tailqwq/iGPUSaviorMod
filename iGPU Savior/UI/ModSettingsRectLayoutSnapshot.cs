namespace PotatoOptimization.UI
{
  internal struct ModSettingsRectLayoutSnapshot
  {
    public ModSettingsRectLayoutSnapshot(
      float anchorMinX,
      float anchorMinY,
      float anchorMaxX,
      float anchorMaxY,
      float pivotX,
      float pivotY,
      float offsetMinX,
      float offsetMinY,
      float offsetMaxX,
      float offsetMaxY)
    {
      AnchorMinX = anchorMinX;
      AnchorMinY = anchorMinY;
      AnchorMaxX = anchorMaxX;
      AnchorMaxY = anchorMaxY;
      PivotX = pivotX;
      PivotY = pivotY;
      OffsetMinX = offsetMinX;
      OffsetMinY = offsetMinY;
      OffsetMaxX = offsetMaxX;
      OffsetMaxY = offsetMaxY;
    }

    public float AnchorMinX { get; }
    public float AnchorMinY { get; }
    public float AnchorMaxX { get; }
    public float AnchorMaxY { get; }
    public float PivotX { get; }
    public float PivotY { get; }
    public float OffsetMinX { get; }
    public float OffsetMinY { get; }
    public float OffsetMaxX { get; }
    public float OffsetMaxY { get; }

    public ModSettingsRectLayoutSnapshot WithHorizontalFrom(ModSettingsRectLayoutSnapshot source)
    {
      return new ModSettingsRectLayoutSnapshot(
        source.AnchorMinX,
        AnchorMinY,
        source.AnchorMaxX,
        AnchorMaxY,
        source.PivotX,
        PivotY,
        source.OffsetMinX,
        OffsetMinY,
        source.OffsetMaxX,
        OffsetMaxY);
    }
  }
}
