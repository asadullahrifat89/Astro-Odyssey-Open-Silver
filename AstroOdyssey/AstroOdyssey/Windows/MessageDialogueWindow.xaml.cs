using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AstroOdyssey
{
    public partial class MessageDialogueWindow : ChildWindow
    {
        readonly Action<bool> _result;

        public MessageDialogueWindow(string title, string message, Action<bool> result = null)
        {
            InitializeComponent();
            _result = result;
            Title = title;
            TextBlock_Message.Text = message;

            if (message.EndsWith("?"))
            {
                OKButton.Content = "Yes";
                CancelButton.Content = "No";
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            _result?.Invoke(true);
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result!.Invoke(false);
            this.DialogResult = false;
        }
    }
}

