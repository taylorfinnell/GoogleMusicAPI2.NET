using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;


namespace Byteopia.Music.GoogleMusicAPI
{
    [DataContract]
    public class GoogleMusicSongUrl
    {
        [DataMember(Name = "url")]
        public String URL { get; set; }
    };

    [DataContract]
    public class AddPlaylistResp
    {
        [DataMember(Name = "id")]
        public String ID { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }
    }

    [DataContract]
    public class DeletePlaylistResp
    {
        [DataMember(Name = "deleteId")]
        public String ID { get; set; }
    }

    [DataContract]
    public class RequestTrackList
    {
        public RequestTrackList(String token)
        {
            ContToken = token;
        }

        [DataMember(Name = "continuationToken")]
        public String ContToken { get; set; }
    }

    [DataContract]
    public class GoogleMusicPlaylists
    {
        [DataMember(Name = "playlists")]
        public List<GoogleMusicPlaylist> UserPlaylists { get; set; }

        [DataMember(Name = "magicPlaylists")]
        public List<GoogleMusicPlaylist> InstantMixes { get; set; }
    }

    [DataContract]
    public class GoogleMusicPlaylist : INotifyPropertyChanged
    {
        string title;
        [DataMember(Name = "title")]
        public string Title
        {
            get { return title; }
            set { title = value; NotifyPropertyChanged("Title"); }
        }

        [DataMember(Name = "playlistId")]
        public string PlaylistID { get; set; }

        [DataMember(Name = "requestTime")]
        public double RequestTime { get; set; }

        [DataMember(Name = "continuationToken")]
        public string ContToken { get; set; }

        [DataMember(Name = "differentialUpdate")]
        public bool DiffUpdate { get; set; }

        [DataMember(Name = "playlist")]
        public List<GoogleMusicSong> Songs { get; set; }

        [DataMember(Name = "continuation")]
        public bool Cont { get; set; }


        public string TrackString
        {
            get { return Songs.Count + " tracks"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }


    [DataContract]
    public class PreviewInfo
    {
        [DataMember(Name = "previewTrackId")]
        public String TrackID { get; set; }

        [DataMember(Name = "previewToken")]
        public String PreviewToken { get; set; }

        [DataMember(Name = "priceText")]
        public String Price { get; set; }

        [DataMember(Name = "purcahseUrl")]
        public String PurchaseURL { get; set; }
    }

    [DataContract]
    public class SharedSongList
    {
        [DataMember(Name = "shares")]
        public List<GoogleMusicSong> Shares { get; set; }
    }

    [DataContract]
    public class ShareInfo
    {
        [DataMember(Name = "postId")]
        public String PostID { get; set; }

        [DataMember(Name = "postText")]
        public String PostText { get; set; }

        [DataMember(Name = "previewListened")]
        public bool HasListened { get; set; }

        [DataMember(Name = "sharedByName")]
        public String SharedByName { get; set; }

        [DataMember(Name = "sharedByPhoto")]
        public String SharedByPhoto { get; set; }

        [DataMember(Name = "sharedByProfileUrl")]
        public String SharedByProfile { get; set; }

    }
    [DataContract]
    public class GoogleMusicSong : INotifyPropertyChanged
    {
        string albumart;
        string albumArtist;
        string album;

        [DataMember(Name = "genre")]
        public string Genre { get; set; }

        [DataMember(Name = "beatsPerMinute")]
        public int BPM { get; set; }

        [DataMember(Name = "albumArtistNorm")]
        public string AlbumArtistNorm { get; set; }

        [DataMember(Name = "artistNorm")]
        public string ArtistNorm { get; set; }

        [DataMember(Name = "album")]
        public string Album
        {
            get { return album; }
            set
            {
                NotifyPropertyChanged("Album");
                album = value;
            }
        }

        [DataMember(Name = "lastPlayed")]
        public double LastPlayed { get; set; }

        [DataMember(Name = "type")]
        public int Type { get; set; }

        [DataMember(Name = "disc")]
        public int Disc { get; set; }

        [DataMember(Name = "id")]
        public string ID { get; set; }

        [DataMember(Name = "composer")]
        public string Composer { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "albumArtist")]
        public string AlbumArtist
        {
            get { return albumArtist; }
            set
            {
                NotifyPropertyChanged("AlbumArtist");
                albumArtist = value;
            }
        }

        [DataMember(Name = "totalTracks")]
        public int TotalTracks { get; set; }

        public String AlbumDetailString
        {
            get
            {
                if (TotalTracks != 0 && !Genre.Equals(String.Empty))
                {
                    return String.Format("{0}, {1} tracks", Genre, TotalTracks);
                }
                else
                {
                    String r = "";
                    if (Genre != "")
                        r += Genre;
                    if (TotalTracks != 0 && Genre != "")
                        r += ", " + TotalTracks + " tracks";
                    if (TotalTracks != 0 && Genre == "")
                        return TotalTracks + " tracks";
                    return r;
                }
            }
        }
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "totalDiscs")]
        public int TotalDiscs { get; set; }

        [DataMember(Name = "year")]
        public int Year { get; set; }

        [DataMember(Name = "titleNorm")]
        public string TitleNorm { get; set; }

        [DataMember(Name = "artist")]
        public string Artist { get; set; }

        [DataMember(Name = "albumNorm")]
        public string AlbumNorm { get; set; }

        [DataMember(Name = "track")]
        public int Track { get; set; }

        [DataMember(Name = "durationMillis")]
        public long Duration { get; set; }

        public String DurationTimeSpan { get { return TimeSpan.FromMilliseconds(this.Duration).ToString("g"); } set {} }

        [DataMember(Name = "albumArt")]
        public string AlbumArt { get; set; }

        [DataMember(Name = "deleted")]
        public bool Deleted { get; set; }

        [DataMember(Name = "url")]
        public string URL { get; set; }

        [DataMember(Name = "creationDate")]
        public float CreationDate { get; set; }

        [DataMember(Name = "playCount")]
        public int Playcount { get; set; }

        [DataMember(Name = "rating")]
        public int Rating { get; set; }

        [DataMember(Name = "comment")]
        public string Comment { get; set; }

        [DataMember(Name = "matchedId")]
        public string MatchedID
        {
            get;
            set;
        }
        [DataMember(Name = "storeId")]
        public string StoreID
        {
            get;
            set;
        }

        [DataMember(Name = "albumArtUrl")]
        public string ArtURL
        {
            get
            {
                return (albumart != null && !albumart.StartsWith("http:")) ? "http:" + albumart : albumart;
            }
            set
            {
                albumart = value;
                NotifyPropertyChanged("ArtURL");

            }
        }

        [DataMember(Name = "previewInfo")]
        public PreviewInfo Preview { get; set; }

        [DataMember(Name = "sharingInfo")]
        public ShareInfo Share;

        public string ArtistAlbum
        {
            get
            {
                return Artist + ", " + Album;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as GoogleMusicSong;
            if (other == null)
            {
                return false;
            }
            return ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public TimeSpan DurationClean
        {
            get
            {
                return TimeSpan.FromMilliseconds(Duration);
            }
        }
    }

    [DataContract]
    public class GoogleRecomendations
    {
        [DataMember(Name = "recommended")]
        public List<GoogleMusicSong> RecommendedSongs;
    }

    [DataContract]
    public class GoogleMusicSearchResults
    {
        [DataMember(Name = "albums")]
        public List<GoogleMusicSong> Albums { get; set; } // Not really a song. fix later

        [DataMember(Name = "artists")]
        public List<GoogleMusicSong> Artists { get; set; }

        [DataMember(Name = "songs")]
        public List<GoogleMusicSong> Songs { get; set; } // Not really a song. fix later
    }

    [DataContract]
    public class GoogleMusicSearch
    {
        [DataMember(Name = "results")]
        public GoogleMusicSearchResults Results { get; set; }
    }

    [DataContract]
    public class GoogleMusicStatus
    {
        [DataMember(Name = "totalTracks")]
        public int TotalTracks { get; set; }

        [DataMember(Name = "availableTracks")]
        public int AvailableTracks { get; set; }
    }

    [DataContract]
    public class Session
    {
        [DataMember(Name = "AuthToken")]
        public String AuthToken { get; set; }

        [DataMember(Name = "Cookies")]
        public List<Cookie> Cookies { get; set; }
    }
}
