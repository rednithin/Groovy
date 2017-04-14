using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Groovy
{
    class Track
    {
        public string Name { get; set; }
        public List<string> Artists { get; set; } = new List<string>();
        public int TrackNumber { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public List<string> Genres = new List<string>();
        public string ImageURL { get; set; }
        public int Year { get; set; }

        public Track(Item item)
        {
            Name = item.Name;
            foreach(var artist in item.Artists)
            {
                Artists.Add(artist.Artist.Name);
            }
            TrackNumber = item.TrackNumber;
            Album = item.Album.Name;
            AlbumArtist = Artists[0];
            foreach(var genre in item.Genres)
            {
                Genres.Add(genre);
            }
            ImageURL = item.ImageUrl;
            Year = Int32.Parse(item.ReleaseDate.Substring(0,4));
        }
    }

    public class Album
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }
    }

    public class ArtistC2
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }
    }

    public class ArtistC
    {
        public string Role { get; set; }
        public ArtistC2 Artist { get; set; }
    }

    public class OtherIds
    {
        public string isrc { get; set; }
    }

    public class Item
    {
        public string ReleaseDate { get; set; }
        public string Duration { get; set; }
        public int TrackNumber { get; set; }
        public bool IsExplicit { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Subgenres { get; set; }
        public List<string> Rights { get; set; }
        public Album Album { get; set; }
        public List<ArtistC> Artists { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public OtherIds OtherIds { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }
        public string Subtitle { get; set; }
    }

    public class Tracks
    {
        public List<Item> Items { get; set; }
        public string ContinuationToken { get; set; }
        public int TotalItemCount { get; set; }
    }

    public class RootObject
    {
        public Tracks Tracks { get; set; }
        public string Culture { get; set; }
    }

    class Groove
    {
        

        public static async Task<List<Track>> Go()
        {
            var client = new HttpClient();

            // Define the data needed to request an authorization token.
            var service = "https://login.live.com/accesstoken.srf";
            var clientId = "fcb2b041-fe82-4c06-8a30-d28a9ddc805d";
            var clientSecret = "uqBChgpbtw2ToiHiTUCk3fN";
            var scope = "app.music.xboxlive.com";
            var grantType = "client_credentials";

            // Create the request data.
            var requestData = new Dictionary<string, string>();
            requestData["client_id"] = clientId;
            requestData["client_secret"] = clientSecret;
            requestData["scope"] = scope;
            requestData["grant_type"] = grantType;

            // Post the request and retrieve the response.
            var response = await client.PostAsync(new Uri(service), new HttpFormUrlEncodedContent(requestData));
            var responseString = await response.Content.ReadAsStringAsync();
            var token = Regex.Match(responseString, ".*\"access_token\":\"(.*?)\".*", RegexOptions.IgnoreCase).Groups[1].Value;

            // Use the token in a new request.
            var request = (HttpWebRequest)WebRequest.Create("https://music.xboxlive.com/1/content/music/search?q=" + "daft+punk" + "&filters=Tracks");
            request.Accept = "application/json";
            request.Headers["Authorization"] = "Bearer " + token;

            WebResponse contentResponse;
            using (contentResponse = await request.GetResponseAsync())
            {
                using (var sr = new StreamReader(contentResponse.GetResponseStream()))
                {
                    responseString = sr.ReadToEnd();
                }
            }
            var rootObject = JsonConvert.DeserializeObject<RootObject>(responseString);
            var tracks =  new List<Track>();
            foreach(var item in rootObject.Tracks.Items)
            {
                var track = new Track(item);
                tracks.Add(track);
            }
            return tracks;
        }

    }
}
