// hardcodet.net NotifyIcon for WPF
// Copyright (c) 2009 Philipp Sumi
// Contact and Information: http://www.hardcodet.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the Code Project Open License (CPOL);
// either version 1.0 of the License, or (at your option) any later
// version.
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// THIS COPYRIGHT NOTICE MAY NOT BE REMOVED FROM THIS FILE


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using Application = System.Windows.Application;
using Timer = System.Threading.Timer;

namespace Hardcodet.Wpf.TaskbarNotification
{
  /// <summary>
  /// A WPF proxy to for a taskbar icon (NotifyIcon) that sits in the system's
  /// taskbar notification area ("system tray").
  /// </summary>
  public partial class TaskbarIcon : FrameworkElement, IDisposable
  {
    #region Members

    /// <summary>
    /// Represents the current icon data.
    /// </summary>
    private NotifyIconData iconData;

    /// <summary>
    /// Receives messages from the taskbar icon.
    /// </summary>
    private readonly WindowMessageSink messageSink;

    /// <summary>
    /// An action that is being invoked if the
    /// <see cref="singleClickTimer"/> fires.
    /// </summary>
    private Action delayedTimerAction;

    /// <summary>
    /// A timer that is used to differentiate between single
    /// and double clicks.
    /// </summary>
    private readonly Timer singleClickTimer;

    /// <summary>
    /// A timer that is used to close open balloon tooltips.
    /// </summary>
    private readonly Timer balloonCloseTimer;

    /// <summary>
    /// Indicates whether the taskbar icon has been created or not.
    /// </summary>
    public bool IsTaskbarIconCreated { get; private set; }

    /// <summary>
    /// Indicates whether custom tooltips are supported, which depends
    /// on the OS. Windows Vista or higher is required in order to
    /// support this feature.
    /// </summary>
    public bool SupportsCustomToolTips
    {
      get { return messageSink.Version == NotifyIconVersion.Vista; }
    }

    /// <summary>
    /// Gets a value indicating whether standard balloons are supported
    /// using <see cref="ShowBalloonTip"/>.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if standard balloons are supported; otherwise, <c>false</c>.
    /// </value>
    public bool SupportsStandardBalloons
    {
      get { return Util.CanShowStandardBalloons(); }
    }



    /// <summary>
    /// Checks whether a non-tooltip popup is currently opened.
    /// </summary>
    private bool IsPopupOpen
    {
      get
      {
        var popup = TrayPopupResolved;
        var menu = ContextMenu;
        var balloon = CustomBalloon;

        return popup != null && popup.IsOpen ||
               menu != null && menu.IsOpen ||
               balloon != null && balloon.IsOpen;

      }
    }

    #endregion


    #region Construction

    /// <summary>
    /// Inits the taskbar icon and registers a message listener
    /// in order to receive events from the taskbar area.
    /// </summary>
    public TaskbarIcon()
    {
      //using dummy sink in design mode
      messageSink = Util.IsDesignMode
                        ? WindowMessageSink.CreateEmpty()
                        : new WindowMessageSink(NotifyIconVersion.Win95);

      //init icon data structure
      iconData = NotifyIconData.CreateDefault(messageSink.MessageWindowHandle);

      //create the taskbar icon
      CreateTaskbarIcon();

      //register event listeners
      messageSink.MouseEventReceived += OnMouseEvent;
      messageSink.TaskbarCreated += OnTaskbarCreated;
      messageSink.ChangeToolTipStateRequest += OnToolTipChange;
      messageSink.BalloonToolTipChanged += OnBalloonToolTipChanged;

      //init single click / balloon timers
      singleClickTimer = new Timer(DoSingleClickAction);
      balloonCloseTimer = new Timer(CloseBalloonCallback);

      //register listener in order to get notified when the application closes
      if (Application.Current != null) Application.Current.Exit += OnExit;
    }

    #endregion


    #region Single Click Timer event

    /// <summary>
    /// Performs a delayed action if the user requested an action
    /// based on a single click of the left mouse.<br/>
    /// This method is invoked by the <see cref="singleClickTimer"/>.
    /// </summary>
    private void DoSingleClickAction(object state)
    {
      if (IsDisposed) return;

      //run action
      Action action = delayedTimerAction;
      if (action != null)
      {
        //cleanup action
        delayedTimerAction = null;

        //switch to UI thread
        this.GetDispatcher().Invoke(action);
      }
    }

    #endregion

    #region Set Version (API)

    /// <summary>
    /// Sets the version flag for the <see cref="iconData"/>.
    /// </summary>
    private void SetVersion()
    {
      iconData.VersionOrTimeout = (uint) NotifyIconVersion.Vista;
      bool status = WinApi.Shell_NotifyIcon(NotifyCommand.SetVersion, ref iconData);

      if (!status)
      {
        iconData.VersionOrTimeout = (uint) NotifyIconVersion.Win2000;
        status = Util.WriteIconData(ref iconData, NotifyCommand.SetVersion);
      }

      if (!status)
      {
        iconData.VersionOrTimeout = (uint) NotifyIconVersion.Win95;
        status = Util.WriteIconData(ref iconData, NotifyCommand.SetVersion);
      }

      if (!status)
      {
        Debug.Fail("Could not set version");
      }
    }

    #endregion

    #region Create / Remove Taskbar Icon

    /// <summary>
    /// Recreates the taskbar icon if the whole taskbar was
    /// recreated (e.g. because Explorer was shut down).
    /// </summary>
    private void OnTaskbarCreated()
    {
      IsTaskbarIconCreated = false;
      CreateTaskbarIcon();
    }


    /// <summary>
    /// Creates the taskbar icon. This message is invoked during initialization,
    /// if the taskbar is restarted, and whenever the icon is displayed.
    /// </summary>
    private void CreateTaskbarIcon()
    {
      lock (this)
      {
        if (!IsTaskbarIconCreated)
        {
          const IconDataMembers members = IconDataMembers.Message
                                          | IconDataMembers.Icon
                                          | IconDataMembers.Tip;

          //write initial configuration
          var status = Util.WriteIconData(ref iconData, NotifyCommand.Add, members);
          if (!status)
          {
            throw new Win32Exception("Could not create icon data");
          }

          //set to most recent version
          SetVersion();
          messageSink.Version = (NotifyIconVersion) iconData.VersionOrTimeout;

          IsTaskbarIconCreated = true;
        }
      }
    }


    /// <summary>
    /// Closes the taskbar icon if required.
    /// </summary>
    private void RemoveTaskbarIcon()
    {
      lock (this)
      {
        if (IsTaskbarIconCreated)
        {
          Util.WriteIconData(ref iconData, NotifyCommand.Delete, IconDataMembers.Message);
          IsTaskbarIconCreated = false;
        }
      }
    }

    #endregion

    #region Dispose / Exit

    /// <summary>
    /// Set to true as soon as <see cref="Dispose"/>
    /// has been invoked.
    /// </summary>
    public bool IsDisposed { get; private set; }


    /// <summary>
    /// Checks if the object has been disposed and
    /// raises a <see cref="ObjectDisposedException"/> in case
    /// the <see cref="IsDisposed"/> flag is true.
    /// </summary>
    private void EnsureNotDisposed()
    {
      if (IsDisposed) throw new ObjectDisposedException(Name ?? GetType().FullName);
    }


    /// <summary>
    /// Disposes the class if the application exits.
    /// </summary>
    private void OnExit(object sender, EventArgs e)
    {
      Dispose();
    }


    /// <summary>
    /// This destructor will run only if the <see cref="Dispose()"/>
    /// method does not get called. This gives this base class the
    /// opportunity to finalize.
    /// <para>
    /// Important: Do not provide destructors in types derived from
    /// this class.
    /// </para>
    /// </summary>
    ~TaskbarIcon()
    {
      Dispose(false);
    }


    /// <summary>
    /// Disposes the object.
    /// </summary>
    /// <remarks>This method is not virtual by design. Derived classes
    /// should override <see cref="Dispose(bool)"/>.
    /// </remarks>
    public void Dispose()
    {
      Dispose(true);

      // This object will be cleaned up by the Dispose method.
      // Therefore, you should call GC.SupressFinalize to
      // take this object off the finalization queue 
      // and prevent finalization code for this object
      // from executing a second time.
      GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Closes the tray and releases all resources.
    /// </summary>
    /// <summary>
    /// <c>Dispose(bool disposing)</c> executes in two distinct scenarios.
    /// If disposing equals <c>true</c>, the method has been called directly
    /// or indirectly by a user's code. Managed and unmanaged resources
    /// can be disposed.
    /// </summary>
    /// <param name="disposing">If disposing equals <c>false</c>, the method
    /// has been called by the runtime from inside the finalizer and you
    /// should not reference other objects. Only unmanaged resources can
    /// be disposed.</param>
    /// <remarks>Check the <see cref="IsDisposed"/> property to determine whether
    /// the method has already been called.</remarks>
    private void Dispose(bool disposing)
    {
      //don't do anything if the component is already disposed
      if (IsDisposed || !disposing) return;

      lock (this)
      {
        IsDisposed = true;

        //deregister application event listener
        if (Application.Current != null)
        {
          Application.Current.Exit -= OnExit;
        }

        //stop timers
        singleClickTimer.Dispose();
        balloonCloseTimer.Dispose();

        //dispose message sink
        messageSink.Dispose();

        //remove icon
        RemoveTaskbarIcon();
      }
    }

    #endregion
  }
}