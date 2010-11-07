using System;
using System.Windows;
using System.Windows.Input;

namespace Hardcodet.Wpf.TaskbarNotification
{
  class CommandRerouter
  {
    private readonly FrameworkElement element;

    public CommandRerouter(FrameworkElement element)
    {
      if (element == null)
      {
        throw new ArgumentNullException("element");
      }

      this.element = element;
    }

    public static void AttachOneShotCommandHandler(FrameworkElement element, RoutedEvent @event, RoutedEventHandler callback)
    {
      RoutedEventHandler handler = null;
      handler = (arg0, arg1) =>
      {
        element.RemoveHandler(@event, handler);

        callback(arg0, arg1);
      };

      element.AddHandler(@event, handler);
    }

    public void AttachCommandHandlers(FrameworkElement sender)
    {
      sender.AddHandler(CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(PreviewCanExecuteHandler), true);
      sender.AddHandler(CommandManager.CanExecuteEvent, new CanExecuteRoutedEventHandler(CanExecuteHandler), true);

      sender.AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(PreviewExecutedHandler), true);
      sender.AddHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(ExecutedHandler), true);
    }
        
    public void DetachCommandHandlers(FrameworkElement sender)
    {
      sender.RemoveHandler(CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(PreviewCanExecuteHandler));
      sender.RemoveHandler(CommandManager.CanExecuteEvent, new CanExecuteRoutedEventHandler(CanExecuteHandler));

      sender.RemoveHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(PreviewExecutedHandler));
      sender.RemoveHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(ExecutedHandler));
    }

    private void PreviewCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
    {
      this.element.RaiseEvent(e);
    }

    private void CanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
    {
      this.element.RaiseEvent(e);
    }

    private void PreviewExecutedHandler(object sender, ExecutedRoutedEventArgs e)
    {
      this.element.RaiseEvent(e);
    }

    private void ExecutedHandler(object sender, ExecutedRoutedEventArgs e)
    {
      this.element.RaiseEvent(e);
    }
  }
}
