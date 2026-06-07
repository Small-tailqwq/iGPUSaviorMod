using PotatoOptimization.UI;
using Xunit;

public class PulldownLayerTransitionTests
{
  [Fact]
  public void ClosingFromOpenDelaysLoweringUntilCloseAnimationCompletes()
  {
    var action = PulldownLayerTransition.GetSortingAction(wasOpen: true, isOpen: false);

    Assert.Equal(PulldownLayerSortingAction.DelayLower, action);
  }

  [Fact]
  public void OpeningRaisesImmediately()
  {
    var action = PulldownLayerTransition.GetSortingAction(wasOpen: false, isOpen: true);

    Assert.Equal(PulldownLayerSortingAction.RaiseNow, action);
  }
}
