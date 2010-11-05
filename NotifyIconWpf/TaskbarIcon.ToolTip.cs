using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace Hardcodet.Wpf.TaskbarNotification {
  public partial class TaskbarIcon {
    /// <summary>
    /// Displays a custom tooltip, if available. This method is only
    /// invoked for Windows Vista and above.
    /// </summary>
    /// <param name="visible">Whether to show or hide the tooltip.</param>
    private void OnToolTipChange(bool visible)
    {
      //if we don't have a tooltip, there's nothing to do here...
      if (TrayToolTipResolved == null) return;

      if (visible)
      {
        if (IsPopupOpen)
        {
          //ignore if we are already displaying something down there
          return;
        }

        var args = RaisePreviewTrayToolTipOpenEvent();
        if (args.Handled) return;

        TrayToolTipResolved.IsOpen = true;

        //raise attached event first
        if (TrayToolTip != null) RaiseToolTipOpenedEvent(TrayToolTip);
        
        //bubble routed event
        RaiseTrayToolTipOpenEvent();
      }
      else
      {
        var args = RaisePreviewTrayToolTipCloseEvent();
        if (args.Handled) return;

        //raise attached event first
        if (TrayToolTip != null) RaiseToolTipCloseEvent(TrayToolTip);

        TrayToolTipResolved.IsOpen = false;

        //bubble event
        RaiseTrayToolTipCloseEvent();
      }
    }


    /// <summary>
    /// Creates a <see cref="System.Windows.Controls.ToolTip"/> control that either
    /// wraps the currently set <see cref="TrayToolTip"/>
    /// control or the <see cref="ToolTipText"/> string.<br/>
    /// If <see cref="TrayToolTip"/> itself is already
    /// a <see cref="System.Windows.Controls.ToolTip"/> instance, it will be used directly.
    /// </summary>
    /// <remarks>We use a <see cref="System.Windows.Controls.ToolTip"/> rather than
    /// <see cref="Popup"/> because there was no way to prevent a
    /// popup from causing cyclic open/close commands if it was
    /// placed under the mouse. ToolTip internally uses a Popup of
    /// its own, but takes advance of Popup's internal <see cref="Popup.HitTestable"/>
    /// property which prevents this issue.</remarks>
    private void CreateCustomToolTip()
    {
      //check if the item itself is a tooltip
      ToolTip tt = TrayToolTip as ToolTip;

      if (tt == null && TrayToolTip != null)
      {
        //create an invisible tooltip that hosts the UIElement
        tt = new ToolTip();
        tt.Placement = PlacementMode.Mouse;

        //do *not* set the placement target, as it causes the popup to become hidden if the
        //TaskbarIcon's parent is hidden, too. At runtime, the parent can be resolved through
        //the ParentTaskbarIcon attached dependency property:
        //tt.PlacementTarget = this;

        //make sure the tooltip is invisible
        tt.HasDropShadow = false;
        tt.BorderThickness = new Thickness(0);
        tt.Background = System.Windows.Media.Brushes.Transparent;

        //setting the 
        tt.StaysOpen = true;
        tt.Content = TrayToolTip;
      }
      else if (tt == null && !String.IsNullOrEmpty(ToolTipText))
      {
        //create a simple tooltip for the string
        tt = new ToolTip();
        tt.Content = ToolTipText;
      }

      //the tooltip explicitly gets the DataContext of this instance.
      //If there is no DataContext, the TaskbarIcon assigns itself
      if (tt != null)
      {
        UpdateDataContext(tt, null, DataContext);
      }

      //store a reference to the used tooltip
      SetTrayToolTipResolved(tt);
    }


    /// <summary>
    /// Sets tooltip settings for the class depending on defined
    /// dependency properties and OS support.
    /// </summary>
    private void WriteToolTipSettings()
    {
      const IconDataMembers flags = IconDataMembers.Tip;
      iconData.ToolTipText = ToolTipText;

      if (messageSink.Version == NotifyIconVersion.Vista)
      {
        //we need to set a tooltip text to get tooltip events from the
        //taskbar icon
        if (String.IsNullOrEmpty(iconData.ToolTipText) && TrayToolTipResolved != null)
        {
          //if we have not tooltip text but a custom tooltip, we
          //need to set a dummy value (we're displaying the ToolTip control, not the string)
          iconData.ToolTipText = "ToolTip";
        }
      }

      //update the tooltip text
      Util.WriteIconData(ref iconData, NotifyCommand.Modify, flags);
    }
  }
}
