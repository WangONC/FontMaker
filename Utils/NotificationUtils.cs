using System;
using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace FontMaker
{
    class NotificationUtils
    {
        public static void showMessage(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        public static void showMessageError(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        public static void showMessageWarning(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        public static bool showMessageBoxOKCancel(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Information);
            return result == System.Windows.MessageBoxResult.OK;
        }

        public static Task<Wpf.Ui.Controls.MessageBoxResult> showMessageWPF(string title, 
            string message,
            string? primary=null,
            string? second = null,
            string? close = null)
        {
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox();
            if(primary != null)
            {
                messageBox.PrimaryButtonText = primary;
            }
            else
            {
                messageBox.IsPrimaryButtonEnabled = false;
            }
            if (second != null)
            {
                messageBox.SecondaryButtonText = second;
            }
            else
            {
                messageBox.IsPrimaryButtonEnabled = false;
            }
            if (close != null)
            {
                messageBox.CloseButtonText = close;
            }
            else
            {
                messageBox.IsCloseButtonEnabled = false;
            }

                // 显示消息框，使用参数 title 和 message
                messageBox.ShowTitle = true;
            messageBox.Title = title;
            messageBox.Content = message;
            return messageBox.ShowDialogAsync();
        }

        public static void showNotification(
            SnackbarPresenter presenter,
            String tietle,
            String message,
            double time,
            ControlAppearance appearance,
            SymbolIcon? icon = null
            )
        {
            // 创建一个新的 Snackbar，传入所需的 presenter 参数
            var snackbar = new Snackbar(presenter);
            snackbar.Title = tietle;
            snackbar.Content = message;
            snackbar.Timeout = TimeSpan.FromMilliseconds(time);
            snackbar.Appearance = appearance;
            // 如果提供了图标，则设置图标
            if (icon != null)
            {
                snackbar.Icon = icon;
            }
            // 显示 Snackbar
            snackbar.ShowAsync();
        }

        public static void showSuccessNotification(
            SnackbarPresenter presenter,
            String tietle,
            String message,
            double time,
            SymbolIcon? icon = null
            )
        {
            if (icon == null)
            {
                // 如果没有提供图标，则使用默认的成功图标
                icon = new SymbolIcon { Symbol = SymbolRegular.CheckmarkCircle32 };
            }
            showNotification(presenter, tietle, message, time, ControlAppearance.Success, icon);

        }

        public static void showSuccessNotification(
        SnackbarPresenter presenter,
        String tietle,
        String message,
        double time
        )
            {
                showNotification(presenter, tietle, message, time, ControlAppearance.Success);

            }


        // 错误通知方法
        public static void showErrorNotification(
            SnackbarPresenter presenter,
            String title,
            String message,
            double time,
            SymbolIcon? icon = null
            )
        {
            if (icon == null)
            {
                // 如果没有提供图标，则使用默认的错误图标
                icon = new SymbolIcon { Symbol = SymbolRegular.DismissCircle32 };
            }
            showNotification(presenter, title, message, time, ControlAppearance.Danger, icon);
        }

        public static void showErrorNotification(
            SnackbarPresenter presenter,
            String title,
            String message,
            double time
            )
        {
            showNotification(presenter, title, message, time, ControlAppearance.Danger);
        }

        // 警告通知方法
        public static void showWarningNotification(
            SnackbarPresenter presenter,
            String title,
            String message,
            double time,
            SymbolIcon? icon = null
            )
        {
            if (icon == null)
            {
                // 如果没有提供图标，则使用默认的警告图标
                icon = new SymbolIcon { Symbol = SymbolRegular.Warning28 };
            }
            showNotification(presenter, title, message, time, ControlAppearance.Caution, icon);
        }

        public static void showWarningNotification(
            SnackbarPresenter presenter,
            String title,
            String message,
            double time
            )
        {
            showNotification(presenter, title, message, time, ControlAppearance.Caution);
        }

        // 信息通知方法
        public static void showInfoNotification(
            SnackbarPresenter presenter,
            String title,
            String message,
            double time,
            SymbolIcon? icon = null
            )
        {
            if (icon == null)
            {
                // 如果没有提供图标，则使用默认的信息图标
                icon = new SymbolIcon { Symbol = SymbolRegular.Info28 };
            }
            showNotification(presenter, title, message, time, ControlAppearance.Info, icon);
        }

        public static void showInfoNotification(
            SnackbarPresenter presenter,
            String title,
            String message,
            double time
            )
        {
            showNotification(presenter, title, message, time, ControlAppearance.Info);
        }

    }

}
