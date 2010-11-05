using System;
using System.Threading;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using Point = System.Windows.Point;

namespace Hardcodet.Wpf.TaskbarNotification
{
	public partial class TaskbarIcon
	{
    /// <summary>
    /// Processes mouse events, which are bubbled
    /// through the class' routed events, trigger
    /// certain actions (e.g. show a popup), or
    /// both.
    /// </summary>
    /// <param name="me">Event flag.</param>
    private void OnMouseEvent(MouseEvent me)
    {
      if (IsDisposed) return;

      switch (me)
      {
        case MouseEvent.MouseMove:
          RaiseTrayMouseMoveEvent();
          //immediately return - there's nothing left to evaluate
          return;
        case MouseEvent.IconRightMouseDown:
          RaiseTrayRightMouseDownEvent();
          break;
        case MouseEvent.IconLeftMouseDown:
          RaiseTrayLeftMouseDownEvent();
          break;
        case MouseEvent.IconRightMouseUp:
          RaiseTrayRightMouseUpEvent();
          break;
        case MouseEvent.IconLeftMouseUp:
          RaiseTrayLeftMouseUpEvent();
          break;
        case MouseEvent.IconMiddleMouseDown:
          RaiseTrayMiddleMouseDownEvent();
          break;
        case MouseEvent.IconMiddleMouseUp:
          RaiseTrayMiddleMouseUpEvent();
          break;
        case MouseEvent.IconDoubleClick:
          //cancel single click timer
          singleClickTimer.Change(Timeout.Infinite, Timeout.Infinite);
          //bubble event
          RaiseTrayMouseDoubleClickEvent();
          break;
        case MouseEvent.BalloonToolTipClicked:
          RaiseTrayBalloonTipClickedEvent();
          break;
        default:
          throw new ArgumentOutOfRangeException("me", "Missing handler for mouse event flag: " + me);
      }


      //get mouse coordinates
      Point cursorPosition = Util.GetCursorPosition();

      bool isLeftClickCommandInvoked = false;
      
      //show popup, if requested
      if (me.IsMatch(PopupActivation))
      {
        if (me == MouseEvent.IconLeftMouseUp)
        {
          //show popup once we are sure it's not a double click
          delayedTimerAction = () =>
                                 {
                                   LeftClickCommand.ExecuteIfEnabled(LeftClickCommandParameter, LeftClickCommandTarget ?? this);
                                   ShowTrayPopup(cursorPosition);
                                 };
          singleClickTimer.Change(WinApi.GetDoubleClickTime(), Timeout.Infinite);
          isLeftClickCommandInvoked = true;
        }
        else
        {
          //show popup immediately
          ShowTrayPopup(cursorPosition);
        }
      }


      //show context menu, if requested
      if (me.IsMatch(MenuActivation))
      {
        if (me == MouseEvent.IconLeftMouseUp)
        {
          //show context menu once we are sure it's not a double click
          delayedTimerAction = () =>
                                 {
                                   LeftClickCommand.ExecuteIfEnabled(LeftClickCommandParameter, LeftClickCommandTarget ?? this);
                                   ShowContextMenu(cursorPosition);
                                 };
          singleClickTimer.Change(WinApi.GetDoubleClickTime(), Timeout.Infinite);
          isLeftClickCommandInvoked = true;
        }
        else
        {
          //show context menu immediately
          ShowContextMenu(cursorPosition);
        }
      }

      //make sure the left click command is invoked on mouse clicks
      if (me == MouseEvent.IconLeftMouseUp && !isLeftClickCommandInvoked)
      {
        //show context menu once we are sure it's not a double click
        delayedTimerAction = () => LeftClickCommand.ExecuteIfEnabled(LeftClickCommandParameter, LeftClickCommandTarget ?? this);
        singleClickTimer.Change(WinApi.GetDoubleClickTime(), Timeout.Infinite);
      }

    }
	}
}
