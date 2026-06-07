namespace PotatoOptimization.UI
{
  internal enum PulldownLayerSortingAction
  {
    None,
    RaiseNow,
    LowerNow,
    DelayLower
  }

  internal static class PulldownLayerTransition
  {
    public static PulldownLayerSortingAction GetSortingAction(bool wasOpen, bool isOpen)
    {
      if (wasOpen == isOpen) return PulldownLayerSortingAction.None;
      return isOpen ? PulldownLayerSortingAction.RaiseNow : PulldownLayerSortingAction.DelayLower;
    }
  }
}
