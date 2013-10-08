﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Windows.Data.Json;
using System.Runtime.Serialization;
using Windows.Storage;
using Windows.Storage.Streams;
using System.IO;

using Byteopia.Helpers;

namespace Byteopia.Music.GoogleMusicAPI
{
    /// <summary>
    /// Super small wrapper for Google's API
    /// </summary>
    /// 
    [DataContract]
    public class API
    {
        public delegate void NotifyChunkAdded(IEnumerable<GoogleMusicSong> songs);
        public event NotifyChunkAdded GetAllSongsChunkAdded;

        public event EventHandler GetAllSongsComplete;

        GoogleHTTP client;

        [DataMember(Name="Client")]
        public Byteopia.Music.GoogleMusicAPI.GoogleHTTP Client
        {
            get { return client; }
            set { client = value; }
        }

        private String _user, _pass;

        [DataMember(Name="Pass")]
        public System.String Pass
        {
            get { return _pass; }
            set { _pass = value; }
        }

        [DataMember(Name="User")]
        public System.String User
        {
            get { return _user; }
            set { _user = value; }
        }

        const String _apiFileName = "api.dat";

        public API()
        {
            client = new GoogleHTTP();
            client.CookiesChanged += client_CookiesChanged;
        }

        public SmartObservableCollection<GoogleMusicSong> Tracks = new SmartObservableCollection<GoogleMusicSong>();
        public ObservableCollection<GoogleMusicPlaylist> Playlists = new ObservableCollection<GoogleMusicPlaylist>();

        public bool HasSession()
        {
            return this.DeseralizeSession(); 
        }

        /// <summary>
        /// Login via email and pw
        /// </summary>
        /// <param name="email">Google music user email</param>
        /// <param name="password">Google music user pass</param>
        /// <returns></returns>
        public async Task<Boolean> Login(String email, String password)
        {
            bool hasSession = this.DeseralizeSession();
            if (hasSession)
                return true;

            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("service", "sj"), // skyjam
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Passwd", password)
            });

            // First hit for auth token
            String loginData = await client.POST(new Uri("https://accounts.google.com/ClientLogin"), content);

            // Bad creds, prolly
            if(client.LastStatusCode == HttpStatusCode.Forbidden)
                return false;

            client.SetAuthToken(loginData);

            // Hit the servers so our cookie container can store the cookies
            await HitForSessionCookies();

            await this.SeralizeSession();

            return true;
        }

        public async Task<int> GetTrackCount()
        {
            GoogleMusicStatus status = await client.POST<GoogleMusicStatus>(new Uri("https://play.google.com/music/services/getstatus"));
            return status.TotalTracks;
        }

        /// <summary>
        /// Gets all songs in music library
        /// </summary>
        /// <param name="continuationToken">Tells Google's servers where to pick up</param>
        /// <returns></returns>
        public async void GetAllSongs(int pagesToFetch = -1)
        {
            GoogleMusicPlaylist playlist = null;
            int pagesFetched = 0;

            // Loop until no more token to continue from
            while (true)
            {
                if (pagesFetched == pagesToFetch)
                    break;

                String jsonString = "{\"continuationToken\":\"" + ((playlist == null) ? "" : playlist.ContToken) + "\"}";
               
                HttpContent content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("json", jsonString),
                });

                playlist = await client.POST<GoogleMusicPlaylist>(new Uri("https://play.google.com/music/services/loadalltracks"), content);

                Tracks.AddRange(playlist.Songs);

                if(this.GetAllSongsChunkAdded != null)
                    this.GetAllSongsChunkAdded(playlist.Songs);

                if (String.IsNullOrEmpty(playlist.ContToken))
                    break;

                pagesFetched++;
            }

            if (this.GetAllSongsComplete != null)
                this.GetAllSongsComplete(this, new EventArgs());
        }

        /// <summary>
        /// Gets complete list of all playlists
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetUserPlaylists()
        {
            GoogleMusicPlaylists playlists = await client.POST<GoogleMusicPlaylists>(new Uri("https://play.google.com/music/services/loadplaylist"));

            if (playlists.UserPlaylists != null)
            {
                foreach (GoogleMusicPlaylist playlist in playlists.UserPlaylists)
                    Playlists.Add(playlist);
            }

            if (playlists.InstantMixes != null)
            {
                foreach (GoogleMusicPlaylist playlist in playlists.InstantMixes)
                    Playlists.Add(playlist);
            }

            return true;
        }

        /// <summary>
        /// Populates the list of songs based on Google's recommendations
        /// </summary>
        public async void GetGoogleRecommendedSongs(ObservableCollection<GoogleMusicSong> googleRecs)
        {
            GoogleRecomendations recs = await client.POST<GoogleRecomendations>(new Uri("https://play.google.com/music/services/recommendedforyou"), null);

            foreach (GoogleMusicSong r in recs.RecommendedSongs)
            {
                googleRecs.Add(r);
            }
        }

        /// <summary>
        /// Gets songs shared with user via G+
        /// </summary>
        public async void GetSharedWithMe(ObservableCollection<GoogleMusicSong> sharedSongs)
        {
            SharedSongList ssl = await client.POST<SharedSongList>(new Uri("https://play.google.com/music/services/sharedwithme"));

            foreach (GoogleMusicSong song in ssl.Shares)
                sharedSongs.Add((song));
        }

        /// <summary>
        /// Adds playlist
        /// </summary>
        /// <param name="playlistName">The playlist to add</param>
        public async Task<bool> AddPlaylist(String playlistName)
        {
            String jsonString = "{\"title\":\"" + playlistName + "\"}";

            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("json", jsonString),
            });

            AddPlaylistResp pl = await client.POST<AddPlaylistResp>(new Uri("https://play.google.com/music/services/addplaylist"), content);

            return pl.Success;
        }

        /// <summary>
        /// Search the music library
        /// </summary>
        /// <param name="query">The query term</param>
        /// <returns></returns>
        public async Task<GoogleMusicSearchResults> Search(String query)
        {
            String jsonString = "{\"q\":\"" + query + "\"}";

            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("json", jsonString),
            });

            GoogleMusicSearch search = await client.POST<GoogleMusicSearch>(new Uri("https://play.google.com/music/services/search"), content);

            return search.Results;
        }
        /// <summary>
        /// Launches the store results page with a given query
        /// </summary>
        /// <param name="query">The query term</param>
        public async void QueryStore(String query)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(String.Format("https://play.google.com/store/search?c=music&feature=music_play_menu&q={0}", query)));
        }

        /// <summary>
        /// Modify a songs meta data
        /// </summary>
        /// <param name="song">Song to be changed</param>
        /// <param name="metaKey">Key to change, ie: rating</param>
        /// <param name="metaValue">Value of key</param>
        /// <returns></returns>
        public async Task<String> ModifySong(GoogleMusicSong song, String metaKey, object metaValue)
        {
            // puke
            String jsonString = "{\"entries\":[{\"id\" : \"" + song.ID + "\", \"" + metaKey +"\":"+"\"" + metaValue + "\"}]}";
            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("json", jsonString),
            });

            return await client.POST(new Uri("https://play.google.com/music/services/modifyentries"), content);
        }

        /// <summary>
        /// Thumbs up a song
        /// </summary>
        /// <param name="song">Song to like</param>
        public async void LikeSong(GoogleMusicSong song)
        {
            await ModifySong(song, "rating", 5);
        }

        /// <summary>
        /// Thumbs down a song
        /// </summary>
        /// <param name="song">Song to hate</param>
        public async void DislikeSong(GoogleMusicSong song)
        {
            await ModifySong(song, "rating", 0);
        }

        /// <summary>
        /// Increment a song's playcount by 1
        /// </summary>
        /// <param name="song">Song to inc playcount</param>
        public async void IncrementPlaycount(GoogleMusicSong song)
        {
            await ModifySong(song, "playCount", song.Playcount + 1);
        }

        /// <summary>
        /// Get song share url
        /// </summary>
        /// <param name="song">song to share</param>
        /// <returns></returns>
        public async Task<String> GetShareableURL(GoogleMusicSong song)
        {
            // Not all songs can be shared, licensing prolly
            if (song.StoreID == null)
                return null;

            String jsonString = "{\"trackId\":\"" + song.StoreID + "\"}";

            HttpContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("json", jsonString),
            });


            GoogleMusicSongUrl url = await client.POST<GoogleMusicSongUrl>(new Uri("https://play.google.com/music/services/shareprepurchasepreview"), content);
            return url.URL;
        }

        /// <summary>
        /// Gets the stream URL from a given song
        /// </summary>
        /// <param name="song">Song to stream</param>
        /// <returns></returns>
        public async Task<String> GetStreamURL(GoogleMusicSong song)
        {
            Uri reqUrl = null;
            // Looks like it's a preview song
            if (song.Preview != null)
            {
                reqUrl = new Uri(String.Format("https://play.google.com/music/playpreview?u=0&mode=streaming&preview={0}&tid={1}&pt=e",
                    song.Preview.PreviewToken, song.Preview.TrackID));
            }
            // Shared song
            else if (song.Share != null)
            {
                reqUrl = new Uri(String.Format("https://play.google.com/music/playpreview?u=0&mode=streaming&preview={0}&tid={1}&postid={2}pt=e",
                    song.Preview.PreviewToken, song.Preview.TrackID, song.Share.PostID));
            }
            // Normal song
            else
            {
                reqUrl = new Uri(String.Format("https://play.google.com/music/play?u=0&songid={0}", song.ID));
            }

            GoogleMusicSongUrl songUrl = null;
            try{
               songUrl = await client.GET<GoogleMusicSongUrl>(reqUrl);
            }
            catch{

            }

            return (songUrl != null) ? songUrl.URL : String.Empty;
        }

        public async Task<bool> HitForSessionCookies()
        {
            String hitForCookies = await client.POST(new Uri("https://play.google.com/music/listen?hl=en&u=0"));
            return true;
        }

        public bool NeedsAuth()
        {
            return client.AuthroizationToken.Equals(String.Empty);
        }

        void client_CookiesChanged(object sender, EventArgs e)
        {
            this.SeralizeSession();
        }

        public async void DeleteFile()
        {
            var dumpFile = await ApplicationData.Current.RoamingFolder.CreateFileAsync(_apiFileName,
               CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(dumpFile, "");
        }

        public async Task<bool> SeralizeLibrary()
        {
            return true;
        }
        public async Task<bool> DeseralizeLibrary()
        {
            return true;
        }

        public async Task<bool> SeralizeSession()
        {
            String googleClient = String.Empty;

            try
            {
                googleClient = JSON.SeralizeObject<Session>(new Session()
                {
                     AuthToken = client.AuthroizationToken,
                     Cookies = client.CookieManager.GetCookiesList()
                });

                Settings.SetSerializedValue("session", googleClient, true);
            }
            catch
            { 
                throw;
                return false; 
            }

            return true;
        }

        public bool DeseralizeSession()
        {
            Session tmp = null;
            try
            {
                tmp = JSON.DeserializeObject<Session>(Settings.GetSerializedStringValue("session", true));

                this.client.AuthroizationToken = tmp.AuthToken;
                this.Client.CookieManager.SetCookiesFromList(tmp.Cookies);
                
            }
            catch
            {
                //throw;
                return false;
            }


            return true;
        }
    }
}