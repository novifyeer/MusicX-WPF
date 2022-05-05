﻿using MusicX.Core.Models;
using MusicX.Core.Services;
using MusicX.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MusicX.ViewModels
{
    public class PlaylistViewModel:BaseViewModel
    {

        public event EventHandler<Playlist> PlaylistLoaded;
        public string Title { get; set; }
        public string ArtistText { get; set; }
        public string Genres { get; set; }
        public string Year { get; set; }
        public string Plays { get; set; }
        public string Description { get; set; }
        public Visibility VisibleLoading { get; set; } = Visibility.Visible;
        public Visibility VisibleContent { get; set; } = Visibility.Collapsed;
        public BitmapImage Cover { get; set; }
        public List<Audio> Tracks { get; set; } = new List<Audio>();

        public Visibility VisibileAddInfo { get; set; } = Visibility.Visible;

        public Playlist Playlist { get; set; }

        private readonly VkService vkService;
        private readonly Logger logger;
        private readonly NotificationsService notificationsService;

        public ConfigService ConfigService { get; set; }

        public PlaylistViewModel(VkService vkService, Logger logger, ConfigService configService, NotificationsService notificationsService)
        {
            this.vkService = vkService;
            this.ConfigService = configService;
            this.logger = logger;

            this.notificationsService = notificationsService;
        }

        public async Task LoadPlaylist(Playlist playlist, bool delete = true)
        {
            try
            {
                logger.Info("Load playlist");
                if (delete)
                {
                    if (Tracks.Count > 0) Tracks.RemoveRange(0, Tracks.Count);

                }
                VisibleContent = Visibility.Collapsed;
                VisibleLoading = Visibility.Visible;

                Changed("VisibleContent");
                Changed("VisibleLoading");

                var p = await vkService.GetPlaylistAsync(100, playlist.Id, playlist.AccessKey, playlist.OwnerId);
                this.PlaylistLoaded.Invoke(this, p.Playlist);


                if (p.Playlist.MainArtists.Count == 0)
                {
                    if(p.Playlist.OwnerId < 0)
                    {
                        if(p.Groups != null)
                        {
                            p.Playlist.OwnerName = p.Groups[0].Name;
                            
                        }
                    }
                }
                playlist = p.Playlist;
                playlist.Audios = p.Audios;
                Tracks = p.Audios; 

                this.Playlist = playlist;
                Title = playlist.Title;
                Year = playlist.Year.ToString();
                Description = playlist.Description;
               
                var genres = string.Empty;
                logger.Info($"load playlist {Playlist.Genres.Count} genres ");
                foreach (var genre in Playlist.Genres)
                {
                    genres += $"{genre.Name}, ";
                }

                if(Playlist.Genres.Count > 0)
                {
                    Genres = genres.Remove(genres.Length - 2);
                }
               
                if (playlist.Cover != null)
                {
                    Cover = new BitmapImage(new Uri(playlist.Cover));

                }

                if (playlist.Year == 0)
                {
                    var date = new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(playlist.UpdateTime);
                    Year = $"Обновлен {date.ToString("dd MMMM")}";
                    Genres = "Подборка";
                    //VisibileAddInfo = Visibility.Collapsed;
                }

                logger.Info($"load {Playlist.MainArtists} artists playlist");
                if (Playlist.MainArtists.Count > 0)
                {
                    string s = string.Empty;
                    foreach (var trackArtist in Playlist.MainArtists)
                    {
                        s += trackArtist.Name + ", ";
                    }

                    var artists = s.Remove(s.Length - 2);

                    ArtistText = artists;
                }
                else
                {
                    ArtistText = playlist.OwnerName;
                }


                if (playlist.Audios.Count == 0)
                {
                    logger.Info("load playlist audios");
                    var res = await vkService.AudioGetAsync(playlist.Id, playlist.OwnerId, playlist.AccessKey).ConfigureAwait(false);

                    Tracks.AddRange(res.Items);
                }

                VisibleContent = Visibility.Visible;
                VisibleLoading = Visibility.Collapsed;

                Changed("Title");
                Changed("Description");
                Changed("ArtistText");
                Changed("VisibileAddInfo");
                Changed("Genres");
                Changed("Year");
                Changed("Plays");
                Changed("Cover");
                Changed("Tracks");
                Changed("VisibleContent");
                Changed("VisibleLoading");

                this.PlaylistLoaded?.Invoke(this, playlist);

            }
            catch (Exception ex)
            {
                logger.Error("Fatal error in load playlist");
                logger.Error(ex, ex.Message);

                notificationsService.Show("Произошла ошибка", "MusicX не смог загрузить контент");

            }
        }

        public async Task LoadPlaylistFromData(long playlistId, long ownerId, string accessKey)
        {
            try
            {
                logger.Info($"Load playlist from data: playlistId = {playlistId}, ownerId = {ownerId}, accessKey = {accessKey}");

                await this.LoadPlaylist(new Playlist() { Id = playlistId, AccessKey = accessKey, OwnerId = ownerId }, true);

                if (Tracks.Count > 0) Tracks.RemoveRange(0, Tracks.Count);
                //VisibleContent = Visibility.Collapsed;
                //VisibleLoading = Visibility.Visible;
                //Changed("VisibleContent");
                //Changed("VisibleLoading");

            }catch (Exception ex)
            {
                logger.Error("Fatal error in load playlist from data");
                logger.Error(ex, ex.Message);

                notificationsService.Show("Произошла ошибка", "MusicX не смог загрузить контент");

            }

        }

        public async Task<bool> AddPlaylist()
        {
            try
            {
                await vkService.AddPlaylistAsync(Playlist.Id, Playlist.OwnerId, Playlist.AccessKey);
                return true;
            }catch(Exception ex)
            {
                logger.Error("Error in add playlist");
                logger.Error(ex, ex.Message);
                notificationsService.Show("Произошла ошибка", "MusicX не смог добавить плейлист");

                return false;
            }
        }

        public async Task<bool> RemovePlaylist()
        {
            try
            {
                await vkService.DeletePlaylistAsync(Playlist.Id, Playlist.OwnerId);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error in remove playlist");
                logger.Error(ex, ex.Message);
                notificationsService.Show("Произошла ошибка", "MusicX не смог удалить плейлист");

                return false;
            }
        }
    }
}
