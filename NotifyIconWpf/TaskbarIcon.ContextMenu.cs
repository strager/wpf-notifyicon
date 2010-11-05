using System.Windows;
using System.Windows.Controls.Primitives;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace Hardcodet.Wpf.TaskbarNotification {
  public partial class TaskbarIcon {
    /// <summary>
    /// Displays the <see cref="System.Windows.Controls.ContextMenu"/> if
    /// it was set.
    /// </summary>
    private void ShowContextMenu(Point screenPosition)
    {
      if (IsDisposed) return;

      //raise preview event no matter whether context menu is currently set
      //or not (enables client to set it on demand)
      var args = RaisePreviewTrayContextMenuOpenEvent();
      if (args.Handled) return;

      if (ContextMenu != null)
      {
        //use absolute position
        ContextMenu.Placement = PlacementMode.AbsolutePoint;
        ContextMenu.HorizontalOffset = screenPosition.X;
        ContextMenu.VerticalOffset = screenPosition.Y;
        ContextMenu.IsOpen = true;

        //activate the message window to track deactivation - otherwise, the context menu
        //does not close if the user clicks somewhere else
        WinApi.SetForegroundWindow(messageSink.MessageWindowHandle);

        //bubble event
        RaiseTrayContextMenuOpenEvent();
      }
    }
  }
}
