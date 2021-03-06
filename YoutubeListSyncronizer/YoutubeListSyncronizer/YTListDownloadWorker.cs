﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Kahia.Common.Extensions.ConversionExtensions;
using Kahia.Common.Extensions.StringExtensions;

namespace YoutubeListSyncronizer
{
    public class YTListDownloadWorker : BackgroundWorker
    {
        public Dictionary<string, string> VideoIDsDictionary { get; private set; }
        public String PlaylistName { get; private set; }
        public int TotalVideoCount { get; private set; }

        private String PlaylistID;

        public YTListDownloadWorker(String playlistID)
        {
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
            VideoIDsDictionary = new Dictionary<string, string>();
            PlaylistID = playlistID;
            TotalVideoCount = 1;
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            try
            {
                DownloadPlaylistItems(PlaylistID);
                TotalVideoCount = VideoIDsDictionary.Count;
                ReportProgress(100);
            }
            catch (Exception ex)
            {
                throw new Exception("YT listesi çekilirken hata oluştu.", ex);
            }
        }

        private void DownloadPlaylistItems(string playlistId)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = "YoutubeListSyncronizer",
                ApiKey = "AIzaSyDgUR4esr5twkPl5jRwGlx6yPGR8e6zBPs"
            });

            //fetch playlist name
            {
                var request = youtubeService.Playlists.List("snippet");
                request.Id = PlaylistID;
                var response = request.Execute();
                try
                {
                    PlaylistName = response.Items[0].Snippet.Title;
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                        throw;
                    PlaylistName = "Youtube";
                }
            }
            ReportProgress(30);

            var nextPageToken = "";
            while (nextPageToken != null)
            {
                var request = youtubeService.PlaylistItems.List("snippet");
                request.PlaylistId = playlistId;
                request.MaxResults = 20;
                request.PageToken = nextPageToken;

                var response = request.Execute();

                foreach (var playlistItem in response.Items)
                {
                    var videoId = playlistItem.Snippet.ResourceId.VideoId;
                    if (VideoIDsDictionary.ContainsKey(videoId))
                        continue;
                    var title = playlistItem.Snippet.Title;
                    VideoIDsDictionary.Add(videoId, title);
                }

                nextPageToken = response.NextPageToken;
            }
        }
    }
}
