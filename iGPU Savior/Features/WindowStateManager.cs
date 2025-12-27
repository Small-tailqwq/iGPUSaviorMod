using System;
using System.Collections;
using UnityEngine;
using PotatoOptimization.Core;
using PotatoOptimization.Utilities;

namespace PotatoOptimization.Features
{
  /// <summary>
  /// 窗口状态管理器 - 负责 PiP 模式和窗口拖动
  /// </summary>
  public class WindowStateManager
  {
    private readonly Configuration.ConfigurationManager _config;

    private bool _isSmallWindow = false;
    private int _currentTargetWidth;
    private int _currentTargetHeight;

    // 原始窗口状态
    private int _origWidth;
    private int _origHeight;
    private FullScreenMode _origMode;
    private IntPtr _origStyle;

    // 右键拖动相关
    private WindowManager.POINT _dragStartScreenPos;
    private Vector2 _dragStartWindowPos;
    private bool _isRightDragging = false;

    public bool IsSmallWindow => _isSmallWindow;

    public WindowStateManager(Configuration.ConfigurationManager config)
    {
      _config = config;

      // 初始记录窗口状态
      _origWidth = Screen.width;
      _origHeight = Screen.height;
      _origMode = Screen.fullScreenMode;

      IntPtr hWnd = WindowManager.GetCurrentWindowHandle();
      _origStyle = WindowManager.GetWindowStyle(hWnd, Constants.GWL_STYLE);
    }

    // 小窗模式记忆位置
    private int _lastPiPX = -10000;
    private int _lastPiPY = -10000;

    /// <summary>
    /// 切换 PiP 模式
    /// </summary>
    public IEnumerator TogglePiPMode()
    {
      IntPtr hWnd = WindowManager.GetCurrentWindowHandle();

      if (!_isSmallWindow)
      {
        // 进入小窗模式
        _origWidth = Screen.width;
        _origHeight = Screen.height;
        _origMode = Screen.fullScreenMode;
        _origStyle = WindowManager.GetWindowStyle(hWnd, Constants.GWL_STYLE);

        _isSmallWindow = true;
        CalculateTargetResolution();
        Screen.SetResolution(_currentTargetWidth, _currentTargetHeight, FullScreenMode.Windowed);

        yield return SetPiPMode(true, _lastPiPX, _lastPiPY);

        // 恢复上次记忆的位置
        if (_lastPiPX > -9000 && _lastPiPY > -9000)
        {
          PotatoPlugin.Log.LogInfo($">>> 恢复小窗位置: ({_lastPiPX}, {_lastPiPY}) <<<");
        }

        PotatoPlugin.Log.LogWarning(">>> 开启画中画 (原始样式已备份) <<<");
      }
      else
      {
        // 记录当前小窗位置
        if (WindowManager.GetWindowBounds(hWnd, out WindowManager.RECT rect))
        {
          _lastPiPX = rect.Left;
          _lastPiPY = rect.Top;
          PotatoPlugin.Log.LogInfo($">>> 记忆小窗位置: ({_lastPiPX}, {_lastPiPY}) <<<");
        }

        // 退出小窗模式
        _isSmallWindow = false;
        Screen.SetResolution(_origWidth, _origHeight, _origMode);

        yield return SetPiPMode(false);

        PotatoPlugin.Log.LogWarning(">>> 恢复原始状态 <<<");
      }
    }

    /// <summary>
    /// 处理拖动逻辑 (在 Update 中调用)
    /// </summary>
    public void HandleDragLogic()
    {
      if (!_isSmallWindow) return;

      DragMode mode = _config.CfgDragMode.Value;

      if (mode == DragMode.Ctrl_LeftClick || mode == DragMode.Alt_LeftClick)
      {
        bool modifierPressed = (mode == DragMode.Ctrl_LeftClick)
            ? (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            : (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

        if (Input.GetMouseButtonDown(0) && modifierPressed)
        {
          WindowManager.StartSystemDrag();
        }
      }
      else if (mode == DragMode.RightClick_Hold)
      {
        if (Input.GetMouseButtonDown(1))
        {
          _isRightDragging = true;
          WindowManager.GetCursorScreenPosition(out _dragStartScreenPos);

          IntPtr hWnd = WindowManager.GetCurrentWindowHandle();
          WindowManager.GetWindowBounds(hWnd, out WindowManager.RECT rect);
          _dragStartWindowPos = new Vector2(rect.Left, rect.Top);
        }

        if (Input.GetMouseButtonUp(1))
        {
          _isRightDragging = false;
        }

        if (_isRightDragging)
        {
          WindowManager.GetCursorScreenPosition(out WindowManager.POINT currentPos);

          int deltaX = currentPos.X - _dragStartScreenPos.X;
          int deltaY = currentPos.Y - _dragStartScreenPos.Y;

          int newX = (int)(_dragStartWindowPos.x + deltaX);
          int newY = (int)(_dragStartWindowPos.y + deltaY);

          WindowManager.MoveWindow(WindowManager.GetCurrentWindowHandle(), newX, newY);
        }
      }
    }

    private void CalculateTargetResolution()
    {
      Resolution screenRes = Screen.currentResolution;
      int divisor = (int)_config.CfgWindowScale.Value;
      _currentTargetWidth = screenRes.width / divisor;
      _currentTargetHeight = screenRes.height / divisor;
    }

    private IEnumerator SetPiPMode(bool enable, int targetX = -10000, int targetY = -10000)
    {
      // 给一帧时间让 Unity 完成窗口化切换
      yield return null;

      try
      {
        IntPtr hWnd = WindowManager.GetCurrentWindowHandle();

        if (enable)
        {
          WindowManager.RemoveWindowBorder(hWnd);

          // 去除边框后立即移动位置 (如果有记忆位置)
          if (targetX > -9000 && targetY > -9000)
          {
            WindowManager.MoveWindow(hWnd, targetX, targetY);
          }
        }
        else
        {
          WindowManager.RestoreWindowStyle(hWnd, _origStyle);
        }
      }
      catch (Exception e)
      {
        PotatoPlugin.Log.LogError($"窗口样式操作失败: {e.Message}");
      }
    }
  }
}
