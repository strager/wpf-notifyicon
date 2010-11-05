using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace Hardcodet.Wpf.TaskbarNotification {
  public partial class TaskbarIcon {
    /// <summary>
    /// Creates a <see cref="ToolTip"/> control that either
    /// wraps the currently set <see cref="TrayToolTip"/>
    /// control or the <see cref="ToolTipText"/> string.<br/>
    /// If <see cref="TrayToolTip"/> itself is already
    /// a <see cref="ToolTip"/> instance, it will be used directly.
    /// </summary>
    /// <remarks>We use a <see cref="ToolTip"/> rather than
    /// <see cref="Popup"/> because there was no way to prevent a
    /// popup from causing cyclic open/close commands if it was
    /// placed under the mouse. ToolTip internally uses a Popup of
    /// its own, but takes advance of Popup's internal <see cref="Popup.HitTestable"/>
    /// property which prevents this issue.</remarks>
    private void CreatePopup()
    {
      //check if the item itself is a popup
      Popup popup = TrayPopup as Popup;

      if (popup == null && TrayPopup != null)
      {
        //create an invisible popup that hosts the UIElement
        popup = new Popup();
        popup.AllowsTransparency = true;

        //don't animate by default - devs can use attached
        //events or override
        popup.PopupAnimation = PopupAnimation.None;

        //the CreateRootPopup method outputs binding errors in the debug window because
        //it tries to bind to "Popup-specific" properties in case they are provided by the child.
        //We don't need that so just assign the control as the child.
        popup.Child = TrayPopup;

        //do *not* set the placement target, as it causes the popup to become hidden if the
        //TaskbarIcon's parent is hidden, too. At runtime, the parent can be resolved through
        //the ParentTaskbarIcon attached dependency property:
        //popup.PlacementTarget = this;

        popup.Placement = PlacementMode.AbsolutePoint;
        popup.StaysOpen = false;
      }

      //the popup explicitly gets the DataContext of this instance.
      //If there is no DataContext, the TaskbarIcon assigns itself
      if (popup != null)
      {
        UpdateDataContext(popup, null, DataContext);
      }

      //store a reference to the used tooltip
      SetTrayPopupResolved(popup);
    }


    /// <summary>
    /// Displays the <see cref="TrayPopup"/> control if
    /// it was set.
    /// </summary>
    private void ShowTrayPopup(Point position)
    {
      if (IsDisposed) return;

      //raise preview event no matter whether popup is currently set
      //or not (enables client to set it on demand)
      var args = RaisePreviewTrayPopupOpenEvent();
      if (args.Handled) return;

      if (TrayPopup != null)
      {
        //use absolute position, but place the popup centered above the icon
        TrayPopupResolved.Placement = PlacementMode.AbsolutePoint;
        TrayPopupResolved.HorizontalOffset = position.X;
        TrayPopupResolved.VerticalOffset = position.Y;

        //open popup
        TrayPopupResolved.IsOpen = true;


        IntPtr handle = IntPtr.Zero;
        if (TrayPopupResolved.Child != null)
        {
          //try to get a handle on the popup itself (via its child)
          HwndSource source = (HwndSource)PresentationSource.FromVisual(TrayPopupResolved.Child);
          if (source != null) handle = source.Handle;
        }

        //if we don't have a handle for the popup, fall back to the message sink
        if (handle == IntPtr.Zero) handle = messageSink.MessageWindowHandle;

        //activate either popup or message sink to track deactivation.
        //otherwise, the popup does not close if the user clicks somewhere else
        WinApi.SetForegroundWindow(handle);

        //raise attached event - item should never be null unless developers
        //changed the CustomPopup directly...
        if (TrayPopup != null) RaisePopupOpenedEvent(TrayPopup);

        //bubble routed event
        RaiseTrayPopupOpenEvent();
      }
    }
  }
}
