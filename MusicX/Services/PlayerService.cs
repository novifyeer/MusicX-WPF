﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MusicX.Core.Models;
using MusicX.Core.Services;
using MusicX.ViewModels;
using NLog;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;

namespace MusicX.Services
{
    public class PlayerService
    {
        private int currentIndex;
        private string blockId;
        private long loadedPlaylistIdTracks;
        public List<Audio> Tracks;
        public Audio CurrentTrack;
        private DispatcherTimer _positionTimer;
        private readonly MediaPlayer player;

        public event EventHandler PlayStateChangedEvent;
        public event EventHandler<TimeSpan> PositionTrackChangedEvent;
        public event EventHandler TrackChangedEvent;

        private readonly VkService vkService;
        private readonly Logger logger;
        private readonly PlaylistViewModel plViewModel;

        public PlayerService(VkService vkService, Logger logger, PlaylistViewModel plViewModel)
        {
            this.vkService = vkService;
            this.logger = logger;
            this.plViewModel = plViewModel;
            player = new MediaPlayer();
            Tracks = new List<Audio>();

            player.AudioCategory = MediaPlayerAudioCategory.Media;


            player.PlaybackSession.PlaybackStateChanged += MediaPlayerOnCurrentStateChanged;
            player.MediaEnded += MediaPlayerOnMediaEnded;
            player.MediaFailed += MediaPlayerOnMediaFailed;

            player.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            player.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;

            player.CommandManager.NextReceived += async (c, e) => await NextTrack();
            player.CommandManager.PreviousReceived += async (c, e) => await PreviousTrack();
            player.CommandManager.PlayReceived += (c, e) => Play();
            player.CommandManager.PauseReceived += (c, e) => Pause();


            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(500);
            _positionTimer.Tick += PositionTimerOnTick;

        }

        public async Task PlayTrack(Audio track)
        {
            try
            {
                logger.Info($"play track {track.Id}");
                if (CurrentTrack?.Id == track.Id) return;
                CurrentTrack = track;
                player.PlaybackSession.Position = TimeSpan.Zero;

                player.Pause();

                var result = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(CurrentTrack.Url));
                var ams = result.MediaSource;

                player.Source = MediaSource.CreateFromAdaptiveMediaSource(ams);

                player.Play();

                TrackChangedEvent?.Invoke(this, EventArgs.Empty);

                PositionTrackChangedEvent?.Invoke(this, TimeSpan.Zero);

                logger.Info($"played track {track.Id}");

                if(track.ParentBlockId != null)
                {
                    if (track.ParentBlockId == this.blockId) return;

                }else
                {
                    if (loadedPlaylistIdTracks == plViewModel.Playlist.Id) return;
                    logger.Info($"Load tracks with playlist id {plViewModel.Playlist.Id}");

                    Tracks = plViewModel.Tracks;
                    loadedPlaylistIdTracks = plViewModel.Playlist.Id;
                    currentIndex = Tracks.IndexOf(Tracks.Single(a => a.Id == track.Id));
                    return;
                }


                await Task.Run(async () =>
                {
                    try
                    {
                        logger.Info("Get current track block info");
                        this.blockId = track.ParentBlockId;
                        var items = await vkService.GetBlockItemsAsync(blockId);

                        Tracks = items.Audios;

                        currentIndex = Tracks.IndexOf(Tracks.Single(a => a.Id == track.Id));
                    }catch (Exception ex)
                    {
                        logger.Error("Error in player service, playTrack => get block items");
                        logger.Error(ex, ex.Message);
                    }
                   
                });
            }catch (Exception ex)
            {
                logger.Error("Error in playerService in PlayTrack:");
                logger.Error(ex, ex.Message);
            }
        }

        public async Task Play(int index, List<Audio> tracks = null)
        {
            try
            {
                logger.Info($"play track with index = {index}");
                currentIndex = index;
                player.PlaybackSession.Position = TimeSpan.Zero;

                player.Pause();
                if (tracks != null) Tracks = tracks;

                CurrentTrack = Tracks[index];

                if (!CurrentTrack.IsAvailable)
                {
                    
                    await NextTrack();
                    return;
                }

                TrackChangedEvent?.Invoke(this, EventArgs.Empty);

                AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(CurrentTrack.Url));
                var ams = result.MediaSource;

                player.Source = MediaSource.CreateFromAdaptiveMediaSource(ams);

                
                player.Play();

                PositionTrackChangedEvent?.Invoke(this, TimeSpan.Zero);

                /* var config  = await _configService.GetConfig();
                config.NowPlayTrack = new NowPlayTrack()
                {
                    Track = CurrentTrack,
                    Album = (Album)CurrentTrack.Album,
                   
                    Index = index,
                    Second = 0
                };
                if (CurrentTrack.Artists.Count > 0) config.NowPlayTrack.Artist = (Core.Models.Artist)CurrentTrack.Artists[0];
               
                await _configService.SetConfig(config);*/

            }
            catch (Exception ex)
            {
                logger.Error("Error in playerService, Play(index)");
                logger.Error(ex, ex.Message);
            }

        }

        public async Task TryPlay()
        {
            try
            {
                logger.Info("Try play track");
                player.PlaybackSession.Position = TimeSpan.Zero;
                player.Pause();

                AdaptiveMediaSourceCreationResult result = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(CurrentTrack.Url));
                var ams = result.MediaSource;
                player.Source = MediaSource.CreateFromAdaptiveMediaSource(ams);
                player.Play();
                PositionTrackChangedEvent?.Invoke(this, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                logger.Error("Error in try play track");
                logger.Error(ex, ex.Message);
                await NextTrack();
            }

        }

        public async Task NextTrack()
        {
            try
            {
                logger.Info("Next track");
                if (currentIndex + 1 > Tracks.Count - 1)
                {

                    return;
                }

                CurrentTrack = Tracks[currentIndex + 1];
                TrackChangedEvent?.Invoke(this, EventArgs.Empty);
                await Play(currentIndex + 1, null);
            }catch(Exception ex)
            {
                logger.Error("Error in playerService => NextTrack");
                logger.Error(ex, ex.Message);
            }
            
        }


        public void Play()
        {
            try
            {
                if (CurrentTrack is null) return;
                player.Play();
            }catch(Exception ex)
            {
                logger.Error("Error in play");
                logger.Error(ex, ex.Message);
            }
            
        }

        public double Volume
        {
            get => player.Volume;
            set
            {
                if (player.Volume == value)
                    return;

                player.Volume = value;
            }
        }

      
        public TimeSpan Position => player.PlaybackSession.Position;
        public TimeSpan Duration => player?.NaturalDuration ?? TimeSpan.Zero;
      

        public bool IsPlaying => player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
             || player.PlaybackSession.PlaybackState == MediaPlaybackState.Opening
             || player.PlaybackSession.PlaybackState == MediaPlaybackState.Buffering;

        

        public void SetTracks(List<Audio> tracks)
        {
            this.Tracks = tracks;
        }

        public async void Pause()
        {
            try
            {
                player.Pause();

            }catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }

        }

        public void SetVolume(double volume)
        {
            Volume = volume;
        }

        public void Seek(TimeSpan position)
        {
            try
            {
                player.PlaybackSession.Position = position;

            }
            catch (Exception e)
            {
                
            }
        }

       

        public async Task PreviousTrack()
        {
            try
            {
                logger.Info("Go to prev track");
                if (Position.TotalSeconds > 10) Seek(TimeSpan.Zero);
                else
                {

                    var index = currentIndex - 1;
                    if (index < 0) index = Tracks.Count - 1;

                    TrackChangedEvent?.Invoke(this, EventArgs.Empty);
                    await Play(index, null);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error in playerService => PreviousTrack");
                logger.Error(e, e.Message);

            }

        }



        private void MediaPlayerOnCurrentStateChanged(MediaPlaybackSession sender, object args)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (sender.PlaybackState == MediaPlaybackState.Playing)
                        _positionTimer.Start();
                    else
                        _positionTimer.Stop();

                    PlayStateChangedEvent?.Invoke(this, EventArgs.Empty);
                });
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
            }

        }

        private async void MediaPlayerOnMediaEnded(MediaPlayer sender, object args)
        {
            try
            {
                await NextTrack();
            }
            catch (Exception e)
            {

                logger.Error(e, e.Message);
            }
        }



        private async void MediaPlayerOnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {



            //if (args.Error == MediaPlayerError.SourceNotSupported)
            //{

            //}

            //Ошибка при загрузке

            if (args.Error == MediaPlayerError.SourceNotSupported)
            {
                logger.Error("Error SourceNotSupported player");

                //audio source url may expire
                await TryPlay();
                return;
            }
            else if (args.Error == MediaPlayerError.NetworkError)
            {
                logger.Error("Network Error player");
            }

            


            //DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            //{
            //    await ContentDialogService.Show(new ExceptionDialog("Невозможно загрузить аудио файл", $"Невозможно загрузить файл по этой причине: {args.Error.ToString()}", new Exception(args.ErrorMessage)));

            //});
            //Log.Error("Media failed. " + args.Error + " " + args.ErrorMessage);
        }

        private void PositionTimerOnTick(object sender, object o)
        {
            Application.Current.Dispatcher.Invoke((() =>
            {
                PositionTrackChangedEvent?.Invoke(this, Position);
            }));
        }
    }
}
