using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Storage;
using Microsoft.Azure.Devices.Client;
using System.Collections.ObjectModel;
using System.Text;

namespace DeviceToCloudSender
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public ObservableCollection<string> FilesInFolder { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public MainPage()
        {
            InitializeComponent();
            FilesInFolder = new ObservableCollection<string>();
            BindingContext = this;
        }

        private async void OnSelectFolderClicked(object sender, EventArgs e)
        {
            var folder = await FolderPicker.PickAsync(default);

            if (folder != null && folder.IsSuccessful)
            {
                SelectedFolder.Text = folder.Folder.Path;

                Directory.GetFiles(folder.Folder.Path).ToList().ForEach(file =>
                {
                    FilesInFolder.Add(file);
                });
            }
        }

        private async void OnBtnSendMessagesClicked(object sender, EventArgs e)
        {
             CancellationTokenSource = new CancellationTokenSource();
            if (String.IsNullOrWhiteSpace(ConnectionString.Text))
            {
                SemanticScreenReader.Announce("ConnectionString is missing!");
                return;
            }

            var deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString.Text);

            bool continueSending = true;

            do
            {
                foreach (var filePath in FilesInFolder)
                {
                    
                    if (CheckBoxAskForEveryMsg.IsChecked)
                    {
                        continueSending = await DisplayAlert("Send Message", $"Send message from {filePath}?", "Yes", "No");
                    }

                    if (!continueSending)
                    {
                        break;
                    }

                    var msg = new Message(Encoding.UTF8.GetBytes(File.ReadAllText(filePath)));

                    await deviceClient.SendEventAsync(msg, CancellationTokenSource.Token);
                }
            }
            while (CheckBoxRepeat.IsChecked && !CancellationTokenSource.Token.IsCancellationRequested
            && continueSending);
        }

        private async void OnBtnStopClicked(object sender, EventArgs e)
        {
            await CancellationTokenSource.CancelAsync();
        }
    }

}
