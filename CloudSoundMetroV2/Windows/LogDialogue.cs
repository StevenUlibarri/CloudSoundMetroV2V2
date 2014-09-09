using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;

namespace CloudSoundMetroV2.Windows
{
    class LogDialogue : BaseMetroDialog
    {
        public LogDialogue()
        {
            this.Loaded += SimpleDialog_Loaded;
        }

        void SimpleDialog_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DialogBody_ContentPresenter.Content != null) //temp fix for #1238. it forces bindings to bind since for some reason, they won't when the dialog is shown.
                ((FrameworkElement)DialogBody_ContentPresenter.Content).InvalidateProperty(FrameworkElement.DataContextProperty);
        }
    }
}
