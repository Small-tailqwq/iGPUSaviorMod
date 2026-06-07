using Cysharp.Threading.Tasks;
using Bulbul;
using UnityEngine;
using PotatoOptimization.Core;

namespace PotatoOptimization.Features
{
    public class PiPGuiManager
    {
        private bool _hiddenByPiP;

        public void OnPiPModeChanged(bool isPiPMode)
        {
            if (isPiPMode)
            {
                if (PotatoPlugin.Config.CfgAutoHideGuiInPiP.Value)
                    TryHideGui();
                return;
            }

            TryRestoreGui();
        }

        private void TryHideGui()
        {
            var showManager = Object.FindObjectOfType<UIShowManagerForPC>();
            if (showManager == null || !showManager.IsShowUI)
                return;

            showManager.AllUIDeactivate().Forget();
            _hiddenByPiP = true;
            PotatoPlugin.Log.LogInfo("小窗模式已自动隐藏 GUI");
        }

        private void TryRestoreGui()
        {
            if (!_hiddenByPiP) return;

            var showManager = Object.FindObjectOfType<UIShowManagerForPC>();
            if (showManager != null)
                showManager.AllUIActivate(true).Forget();

            _hiddenByPiP = false;
            PotatoPlugin.Log.LogInfo("退出小窗模式后已恢复 GUI");
        }
    }
}
