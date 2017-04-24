using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TagLib;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static TagLib.File;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Groovy
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 



    public class FileAbstraction : IFileAbstraction
    {
        public string Name { get; set; }

        public Stream ReadStream { get; set; }

        public Stream WriteStream { get; set; }

        public FileAbstraction(string path, Stream stream)
        {
            Name = path;
            ReadStream = WriteStream = stream;
        }

        public async void CloseStream(Stream stream)
        {
            await ReadStream.FlushAsync();
            ReadStream.Dispose();
        }
    }

    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        ObservableCollection<Track> tracksCollection = new ObservableCollection<Track>();
        String filepath = "";
        StorageFile file = null;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressBarStop();


            /*
            var filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            filePicker.FileTypeFilter.Add(".txt");
            var file = await filePicker.PickSingleFileAsync();
            await Windows.Storage.FileIO.WriteTextAsync(file, myString);
            */

        }

        private async void ShowDialog(String message)
        {
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }

        private void Clear()
        {
            ProgressBarStop();
        }

        private void ProgressBarStart()
        {
            myProgressBar.Visibility = Visibility.Visible;
            myProgressBar.IsEnabled = true;
        }

        private void ProgressBarStop()
        {
            myProgressBar.IsEnabled = false;
            myProgressBar.Visibility = Visibility.Collapsed;
        }

        private void Hamburger_Button_Click(object sender, RoutedEventArgs e)
        {
            mySplitView.IsPaneOpen = !mySplitView.IsPaneOpen;
        }

        private async void searchButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressBarStart();
            try
            {
                tracksCollection.Clear();
                if (searchTextBox.Text == "")
                {
                    return;
                }
                var searchText = searchTextBox.Text;
                searchTextBox.Text = "";
                var tracks = new ObservableCollection<Track>(await Groove.Go(searchText));
                foreach (var track in tracks)
                {
                    tracksCollection.Add(track);
                }
            }
            catch (Exception exception)
            {
                ShowDialog("Cannot connect to the internet. Please try again.");
            }
            ProgressBarStop();
        }

        private void searchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                searchButton_Click(searchButton, e);
            }
        }

        private async void pickFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new FileOpenPicker()
                {
                    SuggestedStartLocation = PickerLocationId.MusicLibrary
                };
                filePicker.FileTypeFilter.Add(".mp3");
                filePicker.FileTypeFilter.Add(".m4a");
                filePicker.FileTypeFilter.Add(".aac");
                filePicker.FileTypeFilter.Add(".flac");
                file = await filePicker.PickSingleFileAsync();
                fileNameTextBlock.Text = file.Path;
                filepath = file.Path;
            }
            catch (Exception exception)
            {
                ShowDialog("Error in picking file. Try storing the file in music directory of your phone."
                    + "\nThen try again.");
            }

        }

        private async Task<byte[]> GetImageFromUrl(string url)
        {
            ProgressBarStart();
            try
            {
                byte[] b = null;
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)(await request.GetResponseAsync());

                if (request.HaveResponse)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Stream receiveStream = response.GetResponseStream();
                        using (var br = new BinaryReader(receiveStream))
                        {
                            b = br.ReadBytes(int.MaxValue / 2);
                        }
                    }
                }
                ProgressBarStop();
                return b;
            }
            catch (Exception exception)
            {
                ShowDialog("Couldn't download image from the internet. Please try again.");
                ProgressBarStop();
            }            
            return null;
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressBarStart();
            try
            {
                if (file == null)
                {
                    ShowDialog("Pick a file first to save tags to. NOTE: Song should be in the music directory.");
                    return;
                }

                int index = tracksGridView.SelectedIndex;
                if (index == -1)
                {
                    ShowDialog("Search for song tags using the search box above. " +
                        "\nThen select the appropriate tags and pick a music file and try again.");
                    return;
                }
                var track = tracksCollection[index];

                var musicfile = await StorageFile.GetFileFromPathAsync(filepath);
                var simpleFileAbstraction = new FileAbstraction(musicfile.Name, await musicfile.OpenStreamForWriteAsync());
                using (var tagFile = Create(simpleFileAbstraction))
                {

                    tagFile.Tag.Clear();
                    tagFile.Tag.Album = track.Album;
                    tagFile.Tag.AlbumArtists = track.Artists.ToArray();
                    tagFile.Tag.Performers = track.Artists.ToArray();
                    tagFile.Tag.Title = track.Name;
                    tagFile.Tag.Track = (uint)track.TrackNumber;
                    tagFile.Tag.TrackCount = (uint)track.TrackNumber;
                    tagFile.Tag.Year = (uint)track.Year;
                    tagFile.Tag.Genres = track.Genres.ToArray();
                    IPicture newArt = new Picture(await GetImageFromUrl(track.ImageURL));
                    tagFile.Tag.Pictures = new IPicture[1] { newArt };

                    //track.ImageURL;
                    tagFile.Save();
                }
                simpleFileAbstraction.CloseStream(simpleFileAbstraction.ReadStream);
                filepath = "";
                file = null;
                ShowDialog("Tags and Album Art Save Successful");
            }
            catch (Exception exception)
            {
                ShowDialog("Error in saving tags to the file. Please try again.");
                ProgressBarStop();
            }
            ProgressBarStop();
        }

        private async void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = myListView.SelectedIndex;
            if (index < 0)
            {
                return;
            }
            myListView.SelectedIndex = -1;
            if (index == 0)
            {
                await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Windows.ApplicationModel.Package.Current.Id.FamilyName)));
            }
            else if (index == 1)
            {
                string url = @"http://myapppolicy.com/app/groovyuwp";
                var uri = new Uri(url);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            else if (index == 2)
            {
                string url = @"https://github.com/rednithin/Groovy";
                var uri = new Uri(url);
                await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            else if (index == 3)
            {
                var emailMessage = new EmailMessage();
                emailMessage.To.Add(new EmailRecipient("reddy.nithinpg@gmail.com"));
                emailMessage.Subject = "Groovy UWP";
                await EmailManager.ShowComposeNewEmailAsync(emailMessage);
            }
        }
    }
}
