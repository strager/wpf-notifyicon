using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using Point = System.Windows.Point;

namespace Hardcodet.Wpf.TaskbarNotification {
  public partial class TaskbarIcon {
    #region Standard Balloon Tips

    /// <summary>
    /// Bubbles events if a balloon ToolTip was displayed
    /// or removed.
    /// </summary>
    /// <param name="visible">Whether the ToolTip was just displayed
    /// or removed.</param>
    private void OnBalloonToolTipChanged(bool visible)
    {
      if (visible)
      {
        RaiseTrayBalloonTipShownEvent();
      }
      else
      {
        RaiseTrayBalloonTipClosedEvent();
      }
    }

    /// <summary>
    /// Displays a balloon tip with the specified title,
    /// text, and icon in the taskbar for the specified time period.
    /// </summary>
    /// <param name="title">The title to display on the balloon tip.</param>
    /// <param name="message">The text to display on the balloon tip.</param>
    /// <param name="symbol">A symbol that indicates the severity.</param>
    public void ShowBalloonTip(string title, string message, BalloonIcon symbol)
    {
      lock (this)
      {
        ShowBalloonTip(title, message, symbol.GetBalloonFlag(), IntPtr.Zero);
      }
    }


    /// <summary>
    /// Displays a balloon tip with the specified title,
    /// text, and a custom icon in the taskbar for the specified time period.
    /// </summary>
    /// <param name="title">The title to display on the balloon tip.</param>
    /// <param name="message">The text to display on the balloon tip.</param>
    /// <param name="customIcon">A custom icon.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="customIcon"/>
    /// is a null reference.</exception>
    public void ShowBalloonTip(string title, string message, Icon customIcon)
    {
      if (customIcon == null) throw new ArgumentNullException("customIcon");

      lock (this)
      {
        ShowBalloonTip(title, message, BalloonFlags.User, customIcon.Handle);
      }
    }


    /// <summary>
    /// Invokes <see cref="WinApi.Shell_NotifyIcon"/> in order to display
    /// a given balloon ToolTip.
    /// </summary>
    /// <param name="title">The title to display on the balloon tip.</param>
    /// <param name="message">The text to display on the balloon tip.</param>
    /// <param name="flags">Indicates what icon to use.</param>
    /// <param name="balloonIconHandle">A handle to a custom icon, if any, or
    /// <see cref="IntPtr.Zero"/>.</param>
    private void ShowBalloonTip(string title, string message, BalloonFlags flags, IntPtr balloonIconHandle)
    {
      EnsureNotDisposed();

      iconData.BalloonText = message ?? String.Empty;
      iconData.BalloonTitle = title ?? String.Empty;

      iconData.BalloonFlags = flags;
      iconData.CustomBalloonIconHandle = balloonIconHandle;
      Util.WriteIconData(ref iconData, NotifyCommand.Modify, IconDataMembers.Info | IconDataMembers.Icon);
    }


    /// <summary>
    /// Hides a balloon ToolTip, if any is displayed.
    /// </summary>
    public void HideBalloonTip()
    {
      EnsureNotDisposed();

      //reset balloon by just setting the info to an empty string
      iconData.BalloonText = iconData.BalloonTitle = String.Empty;
      Util.WriteIconData(ref iconData, NotifyCommand.Modify, IconDataMembers.Info);
    }

    #endregion

    #region Custom Balloons

    /// <summary>
    /// Shows a custom control as a tooltip in the tray location.
    /// </summary>
    /// <param name="balloon"></param>
    /// <param name="animation">An optional animation for the popup.</param>
    /// <param name="timeout">The time after which the popup is being closed.
    /// Submit null in order to keep the balloon open inde
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="balloon"/>
    /// is a null reference.</exception>
    public void ShowCustomBalloon(UIElement balloon, PopupAnimation animation, int? timeout)
    {
      Dispatcher dispatcher = this.GetDispatcher();
      if (!dispatcher.CheckAccess())
      {
        var action = new Action(() => ShowCustomBalloon(balloon, animation, timeout));
        dispatcher.Invoke(DispatcherPriority.Normal, action);
        return;
      }

      if (balloon == null) throw new ArgumentNullException("balloon");
      if (timeout.HasValue && timeout < 500)
      {
        string msg = "Invalid timeout of {0} milliseconds. Timeout must be at least 500 ms";
        msg = String.Format(msg, timeout); 
        throw new ArgumentOutOfRangeException("timeout", msg);
      }

      EnsureNotDisposed();

      //make sure we don't have an open balloon
      lock (this)
      {
        CloseBalloon();
      }
      
      //create an invisible popup that hosts the UIElement
      Popup popup = new Popup();
      popup.AllowsTransparency = true;

      //provide the popup with the taskbar icon's data context
      UpdateDataContext(popup, null, DataContext);

      //don't animate by default - devs can use attached
      //events or override
      popup.PopupAnimation = animation;

      popup.Child = balloon;

      //don't set the PlacementTarget as it causes the popup to become hidden if the
      //TaskbarIcon's parent is hidden, too...
      //popup.PlacementTarget = this;
      
      popup.Placement = PlacementMode.AbsolutePoint;
      popup.StaysOpen = true;

      Point position = Util.ScreenPositionToDiuPoint(TrayInfo.GetTrayLocation());
      popup.HorizontalOffset = position.X - 1;
      popup.VerticalOffset = position.Y - 1;

      //store reference
      lock (this)
      {
        SetCustomBalloon(popup);
      }

      //assign this instance as an attached property
      SetParentTaskbarIcon(balloon, this);

      //fire attached event
      RaiseBalloonShowingEvent(balloon, this);

      //display item
      popup.IsOpen = true;

      if (timeout.HasValue)
      {
        //register timer to close the popup
        balloonCloseTimer.Change(timeout.Value, Timeout.Infinite);
      }
    }


    /// <summary>
    /// Resets the closing timeout, which effectively
    /// keeps a displayed balloon message open until
    /// it is either closed programmatically through
    /// <see cref="CloseBalloon"/> or due to a new
    /// message being displayed.
    /// </summary>
    public void ResetBalloonCloseTimer()
    {
      if (IsDisposed) return;

      lock (this)
      {
        //reset timer in any case
        balloonCloseTimer.Change(Timeout.Infinite, Timeout.Infinite);
      }
    }


    /// <summary>
    /// Closes the current <see cref="CustomBalloon"/>, if the
    /// property is set.
    /// </summary>
    public void CloseBalloon()
    {
      if (IsDisposed) return;

      Dispatcher dispatcher = this.GetDispatcher();
      if (!dispatcher.CheckAccess())
      {
        Action action = CloseBalloon;
        dispatcher.Invoke(DispatcherPriority.Normal, action);
        return;
      }

      lock (this)
      {
        //reset timer in any case
        balloonCloseTimer.Change(Timeout.Infinite, Timeout.Infinite);

        //reset old popup, if we still have one
        Popup popup = CustomBalloon;
        if (popup != null)
        {
          UIElement element = popup.Child;

          //announce closing
          RoutedEventArgs eventArgs = RaiseBalloonClosingEvent(element, this);
          if (!eventArgs.Handled)
          {
            //if the event was handled, clear the reference to the popup,
            //but don't close it - the handling code has to manage this stuff now

            //close the popup
            popup.IsOpen = false;

            //reset attached property
            if (element != null) SetParentTaskbarIcon(element, null);
          }

          //remove custom balloon anyway
          SetCustomBalloon(null);
        }
      }
    }


    /// <summary>
    /// Timer-invoke event which closes the currently open balloon and
    /// resets the <see cref="CustomBalloon"/> dependency property.
    /// </summary>
    private void CloseBalloonCallback(object state)
    {
      if (IsDisposed) return;

      //switch to UI thread
      Action action = CloseBalloon;
      this.GetDispatcher().Invoke(action);
    }

    #endregion
  }
}
