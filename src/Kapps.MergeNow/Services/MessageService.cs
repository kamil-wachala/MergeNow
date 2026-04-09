using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace MergeNow.Services
{
    public class MessageService : IMessageService
    {
        private readonly AsyncPackage _package;

        public MessageService(AsyncPackage package)
        {
            _package = package;
        }

        public void ShowMessage(string message) => Show(message, OLEMSGICON.OLEMSGICON_INFO);

        public void ShowWarning(string message) => Show(message, OLEMSGICON.OLEMSGICON_WARNING);

        public void ShowError(string message)
        {
            Logger.Error(message);
            Show(message, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        public void ShowError(Exception exception)
        {
            Logger.Error(exception);
            Show(exception?.Message, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        private void Show(string message, OLEMSGICON icon)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (ThreadHelper.CheckAccess())
            {
                VsShellUtilities.ShowMessageBox(_package, message, null, icon,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
                VsShellUtilities.ShowMessageBox(_package, message, null, icon,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            });
        }
    }
}
