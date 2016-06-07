﻿using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using VLC_WinRT.MediaMetaFetcher;
using VLC_WinRT.Model.Video;
using VLC_WinRT.Utils;
using VLC_WinRT.ViewModels;
using Windows.Storage;
using System.Linq;

namespace VLC_WinRT.Services.RunTime
{
    public sealed class VideoMetaService
    {
        readonly VideoMDFetcher videoMdFetcher = new VideoMDFetcher(App.ApiKeyMovieDb);

        public async Task<bool> GetMovieSubtitle(VideoItem video)
        {
            if (Locator.MainVM.IsInternet && !string.IsNullOrEmpty(video.Path))
            {
                var bytes = await videoMdFetcher.GetMovieSubtitle(video);
                
                if (bytes != null)
                {
                    var success = await SaveMovieSubtitleAsync(video, bytes);
                    if (success)
                    {
                        if (video.Id > -1)
                        {
                            await Locator.MediaLibrary.UpdateVideo(video);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> SaveMovieSubtitleAsync(VideoItem video, byte[] sub)
        {
            if (await FetcherHelpers.SaveBytes(video.Id, "movieSub", sub, "zip", true))
            {
                var ext = await FetcherHelpers.ExtractFromArchive(video.Id, $"{ApplicationData.Current.TemporaryFolder.Path}\\{video.Id}.zip");
                if (!string.IsNullOrEmpty(ext))
                {
                    await DispatchHelper.InvokeAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        video.IsSubtitlePreLoaded = true;
                        video.SubtitleExtension = ext;
                    });
                    return true;
                }
            }
            return false;
        }

        
        public async Task<bool> GetMoviePicture(VideoItem video)
        {
            if (Locator.MainVM.IsInternet && !string.IsNullOrEmpty(video.Name))
            {
                var bytes = await videoMdFetcher.GetMovieCover(video.Name);

                if (bytes != null)
                {
                    var success = await SaveMoviePictureAsync(video, bytes);
                    if (success)
                    {
                        await Locator.MediaLibrary.UpdateVideo(video);
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> SaveMoviePictureAsync(VideoItem video, byte[] img)
        {
            if (await FetcherHelpers.SaveBytes(video.Id, "moviePic", img, "jpg", false))
            {
                await DispatchHelper.InvokeAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    video.IsPictureLoaded = true;
                    video.HasMoviePicture = true;
                });
                await video.ResetVideoPicture();
                return true;
            }
            return false;
        }
    }
}
