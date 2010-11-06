using System.Windows;
using System.Windows.Input;

namespace Hardcodet.Wpf.TaskbarNotification {
    public partial class TaskbarIcon {
        private static void AddOneShotHandler(FrameworkElement element, RoutedEvent @event, RoutedEventHandler callback) {
            RoutedEventHandler handler = null;
            handler = (arg0, arg1) => {
                element.RemoveHandler(@event, handler);

                callback(arg0, arg1);
            };

            element.AddHandler(@event, handler);
        }

        private void AttachCommandHandlers(FrameworkElement element) {
            if (element == null) {
                return;
            }

            element.AddHandler(CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(PreviewCanExecuteHandler), true);
            element.AddHandler(CommandManager.CanExecuteEvent, new CanExecuteRoutedEventHandler(CanExecuteHandler), true);

            element.AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(PreviewExecutedHandler), true);
            element.AddHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(ExecutedHandler), true);
        }
        
        private void DetachCommandHandlers(FrameworkElement element) {
            if (element == null) {
                return;
            }

            element.RemoveHandler(CommandManager.PreviewCanExecuteEvent, new CanExecuteRoutedEventHandler(PreviewCanExecuteHandler));
            element.RemoveHandler(CommandManager.CanExecuteEvent, new CanExecuteRoutedEventHandler(CanExecuteHandler));

            element.RemoveHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(PreviewExecutedHandler));
            element.RemoveHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(ExecutedHandler));
        }

        private void PreviewCanExecuteHandler(object sender, CanExecuteRoutedEventArgs e) {
            RaiseEvent(e);
        }

        private void CanExecuteHandler(object sender, CanExecuteRoutedEventArgs e) {
            RaiseEvent(e);
        }

        private void PreviewExecutedHandler(object sender, ExecutedRoutedEventArgs e) {
            RaiseEvent(e);
        }

        private void ExecutedHandler(object sender, ExecutedRoutedEventArgs e) {
            RaiseEvent(e);
        }
    }
}
