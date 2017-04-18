using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.Pickers;
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
            if(searchTextBox.Text == "")
            {
                return;
            }
            var searchText = searchTextBox.Text;
            searchTextBox.Text = "";
            var tracks = new ObservableCollection<Track>(await Groove.Go(searchText));
            foreach(var track in tracks)
            {
                tracksCollection.Add(track);
            }
        }

        private void searchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                searchButton_Click(searchButton, e);
            }
        }

        private async void pickFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker()
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".m4a");
            filePicker.FileTypeFilter.Add(".aac");
            filePicker.FileTypeFilter.Add(".flac");
            file = await filePicker.PickSingleFileAsync();
            fileNameTextBlock.Text = file.Path;
            filepath = file.Path;
        }

        private async void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if(file == null)
            {
                return;
            }
            
            int index = tracksGridView.SelectedIndex;
            if (index == -1)
            {
                return;
            }
            var track = tracksCollection[index];

            var musicfile = await StorageFile.GetFileFromPathAsync(filepath);
            var simpleFileAbstraction = new FileAbstraction(musicfile.Name, await musicfile.OpenStreamForWriteAsync());
            using (var tagFile = TagLib.File.Create(simpleFileAbstraction))
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


                //track.ImageURL;
                tagFile.Save();
            }
            
            filepath = "";
            file = null;
        }
    }
}
