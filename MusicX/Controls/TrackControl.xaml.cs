﻿using DryIoc;
using MusicX.Core.Models;
using MusicX.Core.Services;
using MusicX.Services;
using MusicX.Views;
using MusicX.Views.Modals;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AsyncAwaitBestPractices;
using MusicX.Helpers;
using MusicX.ViewModels;
using WpfAnimatedGif;
using MusicX.ViewModels.Modals;

namespace MusicX.Controls
{
    /// <summary>
    /// Логика взаимодействия для TrackControl.xaml
    /// </summary>
    public partial class TrackControl : UserControl
    {
        private readonly Logger logger;
        private readonly PlayerService player;
        
        public BitmapImage BitImage { get; set; }

        public TrackControl()
        {
            InitializeComponent();
            logger = StaticService.Container.Resolve<Logger>();
            player = StaticService.Container.Resolve<PlayerService>();


            player.TrackChangedEvent += Player_TrackChangedEvent;
            player.PlayStateChangedEvent += Player_PlayStateChangedEvent;

        }

        private void UpdatePlayingAnimation(bool autoStart)
        {
            if (ImageBehavior.GetAnimationController(PanelAnim) is { } controller)
            {
                if (autoStart)
                {
                    controller.Play();
                }
                else
                {
                    controller.Pause();
                    controller.GotoFrame(0);
                }
                return;
            }
            
            ImageBehavior.SetAutoStart(PanelAnim, autoStart);
            ImageBehavior.SetAnimatedSource(PanelAnim, new BitmapImage(new("../Assets/play.gif", UriKind.Relative)));
        }

        private void Player_PlayStateChangedEvent(object? sender, EventArgs e)
        {
            if(player.CurrentTrack != null && player.CurrentTrack.Id == this.Audio.Id)
            {
                this.IconPlay.Symbol = player.IsPlaying ? Wpf.Ui.Common.SymbolRegular.Pause24 : Wpf.Ui.Common.SymbolRegular.Play24;
                UpdatePlayingAnimation(player.IsPlaying);
            }
        }

        private void Player_TrackChangedEvent(object? sender, EventArgs e)
        {
            if(player.CurrentTrack.Id == this.Audio.Id)
            {

                if(!ShowCard)
                {
                    Card.Opacity = 1;
                }


                PlayButtons.Visibility = Visibility.Visible;
                IconPlay.Visibility = Visibility.Collapsed;

                this.IconPlay.Visibility = Visibility.Collapsed;

                UpdatePlayingAnimation(player.IsPlaying);
                PanelAnim.Visibility = Visibility.Visible;
            }
            else
            {
                PlayButtons.Visibility = Visibility.Collapsed;

                IconPlay.Visibility = Visibility.Visible;
                ImageBehavior.GetAnimationController(PanelAnim)?.Pause();
                PanelAnim.Visibility = Visibility.Collapsed;

                if (!ShowCard)
                {
                    Card.Opacity = 0;
                }




                this.IconPlay.Visibility = Visibility.Visible;
                this.IconPlay.Symbol = Wpf.Ui.Common.SymbolRegular.Play24;
            }
        }

        public static readonly DependencyProperty LoadOtherTracksProperty = DependencyProperty.Register(
            "LoadOtherTracks", typeof(bool), typeof(TrackControl), new PropertyMetadata(true));

        public bool LoadOtherTracks
        {
            get => (bool)GetValue(LoadOtherTracksProperty);
            set => SetValue(LoadOtherTracksProperty, value);
        }

        public static readonly DependencyProperty ShowCardProperty =
            DependencyProperty.Register("ShowCard", typeof(bool), typeof(TrackControl), new PropertyMetadata(true));

        public bool ShowCard
        {
            get { return (bool)GetValue(ShowCardProperty); }
            set
            {
                SetValue(ShowCardProperty, value);
            }
        }

        public static readonly DependencyProperty ChartPositionProperty =
            DependencyProperty.Register("ChartPosition", typeof(int), typeof(TrackControl), new PropertyMetadata(0));

        public int ChartPosition
        {
            get { return (int)GetValue(ChartPositionProperty); }
            set
            {
                SetValue(ChartPositionProperty, value);
            }
        }

        public static readonly DependencyProperty AudioProperty =
          DependencyProperty.Register("Audio", typeof(Audio), typeof(TrackControl), new PropertyMetadata(new Audio()));

        public Audio Audio
        {
            get { return (Audio)GetValue(AudioProperty); }
            set
            {
                SetValue(AudioProperty, value);
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                this.IconPlay.Symbol = Wpf.Ui.Common.SymbolRegular.Play24;

                if (ShowCard)
                {
                    this.Card.Opacity = 1;
                }
                else
                {
                    TextsPanel.MaxWidth = double.PositiveInfinity;
                    this.Card.Opacity = 0;
                }

                Subtitle.Visibility = string.IsNullOrEmpty(Audio.Subtitle) ? Visibility.Collapsed : Visibility.Visible;

                Title.Text = Audio.Title;
                Subtitle.Text = Audio.Subtitle;
                if (ChartPosition != 0)
                {
                    ChartGrid.Visibility = Visibility.Visible;
                    ChartPositionText.Text = ChartPosition.ToString();
                }

                if(!Audio.IsAvailable || Audio.Url == String.Empty)
                {
                    Title.Text = Audio.Title;
                    Subtitle.Text = Audio.Subtitle;
                    Artists.Text = Audio.Artist;
                    this.Opacity = 0.3;
                    return;
                }

              
                if(BitImage != null)
                {
                    Cover.ImageSource = BitImage;
                }else
                {
                    if (Audio.Album != null)
                    {
                        if (Audio.Album.Cover != null)
                            Cover.ImageSource = new BitmapImage(new Uri(Audio.Album.Cover))
                            {
                                DecodePixelHeight = 45, 
                                DecodePixelWidth = 45, 
                                UriCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default)
                            };

                    }
                }

                var time = string.Empty;
                TimeSpan t = TimeSpan.FromSeconds(Audio.Duration);
                if (t.Hours > 0) time = t.ToString("h\\:mm\\:ss");
                time = t.ToString("m\\:ss");


                Time.Text = time;

                if (Audio.MainArtists is null or {Count: 0})
                {
                    Artists.Text = Audio.Artist;
                    Artists.MouseEnter += Artists_MouseEnter;
                    Artists.MouseLeave += Artists_MouseLeave;
                    Artists.MouseLeftButtonDown += Artists_MouseLeftButtonDown;
                    
                    AddArtistContextMenu(Audio.Artist, Audio.Artist);
                }
                else
                {
                    Artists.Inlines.Clear();
                    AddArtists(Audio.MainArtists);
                }
                
                if (Audio.FeaturedArtists?.Count > 0)
                    Artists.Inlines.Add(" feat. ");
                if (Audio.FeaturedArtists is not null)
                    AddArtists(Audio.FeaturedArtists);
                
                


                var configService = StaticService.Container.Resolve<Services.ConfigService>();


                var config = await configService.GetConfig();

                if (Audio.OwnerId == config.UserId)
                {
                    AddRemoveIcon.Symbol = Wpf.Ui.Common.SymbolRegular.Delete20;
                    AddRemoveText.Text = "Удалить";
                }
                else
                {
                    AddRemoveIcon.Symbol = Wpf.Ui.Common.SymbolRegular.Add24;
                    AddRemoveText.Text = "Добавить к себе";

                }


                if(Audio.IsExplicit)
                {
                    explicitBadge.Visibility = Visibility.Visible;
                }else
                {
                    explicitBadge.Visibility = Visibility.Collapsed;

                }

               
                if(this.ActualWidth > 110)
                {
                    NamePanel.MaxWidth = this.ActualWidth - 110;

                    if (Audio.IsExplicit)
                    {
                        Title.MaxWidth = (NamePanel.MaxWidth - 20);

                    }
                    else
                    {
                        Title.MaxWidth = (NamePanel.MaxWidth);
                    }

                    Artists.MaxWidth = this.ActualWidth;
                }


                if (player.CurrentTrack != null && player.CurrentTrack.Id == this.Audio.Id)
                {
                    PlayButtons.Visibility = Visibility.Visible;
                    IconPlay.Visibility = Visibility.Collapsed;

                    this.IconPlay.Visibility = Visibility.Collapsed;
                    UpdatePlayingAnimation(player.IsPlaying);
                    PanelAnim.Visibility = Visibility.Visible;

                    if (!ShowCard)
                    {
                        Card.Opacity = 1;
                    }
                }
                  


            }
            catch (Exception ex)
            {
                logger.Error("Failed load track control");
                logger.Error(ex, ex.Message);

                Title.Text = "Невозможно загрузить";
                Subtitle.Text = "это аудио";

                Artists.Text = "Попробуйте позже";
            }
            
        }
        private void AddArtists(IEnumerable<MainArtist> artists)
        {
            var first = true;
            foreach (var artist in artists)
            {
                if (first)
                    first = false;
                else
                    Artists.Inlines.Add(", ");
                        
                var textBlock = new TextBlock
                {
                    Text = artist.Name,
                    DataContext = artist
                };
                    
                textBlock.MouseEnter += Artists_MouseEnter;
                textBlock.MouseLeave += Artists_MouseLeave;
                textBlock.MouseLeftButtonDown += Artists_MouseLeftButtonDown;
                    
                Artists.Inlines.Add(textBlock);

                AddArtistContextMenu(artist.Name, artist.Id);
            }
        }
        private void AddArtistContextMenu(string artistName, string id)
        {
            var text = new TextBlock { Text = artistName, Tag = id, Foreground = Brushes.White };
            text.MouseLeftButtonDown += Text_MouseLeftButtonDown;
            GoToArtistMenu.Items.Add(text);
        }

        private async void Text_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var navigationService = StaticService.Container.Resolve<Services.NavigationService>();

                if (Audio.MainArtists == null)
                {
                    navigationService.OpenSection(Audio.Artist, SectionType.Search);
                }
                else
                {
                    navigationService.OpenSection((string)((TextBlock)sender).Tag, SectionType.Artist);
                }
            }catch(Exception ex)
            {
                logger.Error(ex, ex.Message);
                StaticService.Container.Resolve<NotificationsService>().Show("Ошибка", "Нам не удалось перейти на эту секцию");
            }

        }


        double oldWidth = 0;
        double oldWidthArtists = 0;

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                RecommendedAudio.Visibility = Visibility.Visible;



                if(player.CurrentTrack == null || player.CurrentTrack.Id != this.Audio.Id)
                {
                    PlayButtons.Visibility = Visibility.Visible;
                }

                if (ShowCard)
                {
                    oldWidth = Title.ActualWidth;
                    oldWidthArtists = Artists.ActualWidth;
                    Title.MaxWidth = 120;
                    Subtitle.Visibility = Visibility.Collapsed;
                    Artists.MaxWidth = 120;

                    explicitBadge.Margin = new Thickness(7, 0, 0, 0);

                }

              

             
                if (!ShowCard)
                {
                    if(player.CurrentTrack == null || player.CurrentTrack.Id != this.Audio.Id)
                    {
                        Card.Opacity = 1;

                    }

                }


                Card.Opacity = 0.5;
            }catch(Exception ex)
            {
                logger.Error(ex, ex.Message);

            }

        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                RecommendedAudio.Visibility = Visibility.Collapsed;

               

                if (player.CurrentTrack == null || player.CurrentTrack.Id != this.Audio.Id)
                {
                    PlayButtons.Visibility = Visibility.Collapsed;
                }

                if (ShowCard)
                {
                    Title.MaxWidth = oldWidth + 2;
                    Subtitle.Visibility = Visibility.Visible;

                    Artists.MaxWidth = oldWidthArtists + 2;

                    explicitBadge.Margin = new Thickness(0, 0, 0, 0);
                    Card.Opacity = 1;
                }
                
                if (player.CurrentTrack == null || player.CurrentTrack.Id != this.Audio.Id)
                {
                    Card.Opacity = ShowCard ? 1 : 0;
                }
            }catch(Exception ex)
            {
                logger.Error(ex, ex.Message);

            }
        }

        private async void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.Source is TextBlock)
                    return;
                
                if (Audio.Url == String.Empty)
                {
                    var navigationService = StaticService.Container.Resolve<Services.NavigationService>();
                    var modalViewModel = StaticService.Container.Resolve<TrackNotAvalibleModalViewModel>();
                    await modalViewModel.LoadAsync(Audio.TrackCode, Audio.OwnerId + "_" + Audio.Id + "_" + Audio.AccessKey);
                    
                    navigationService.OpenModal<TrackNotAvalibleModal>(modalViewModel);
                    return;
                }

                if (this.FindAncestor<PlaylistView>() is {DataContext: PlaylistViewModel viewModel})
                    player.CurrentPlaylist = viewModel.PlaylistData;
                
                await player.PlayTrack(Audio, LoadOtherTracks);
            }catch(Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
           
        }

        private void Artists_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is not TextBlock block)
                    return;
                block.TextDecorations.Add(TextDecorations.Underline);
                this.Cursor = Cursors.Hand;
            }catch (Exception ex)
            {
                logger.Error(ex, ex.Message);

            }

        }

        private void Artists_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is not TextBlock block)
                    return;
                foreach (var dec in TextDecorations.Underline)
                {
                    block.TextDecorations.Remove(dec);
                }
                this.Cursor = Cursors.Arrow;
            }catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }

        }

        private async void Artists_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var navigationService = StaticService.Container.Resolve<Services.NavigationService>();

                if (Audio.MainArtists == null)
                {
                    navigationService.OpenSection(Audio.Artist, SectionType.Search);
                }
                else if (sender is FrameworkElement {DataContext: MainArtist artist})
                {
                    navigationService.OpenSection(artist.Id, SectionType.Artist);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                StaticService.Container.Resolve<NotificationsService>().Show("Ошибка", "Нам не удалось перейти на эту секцию");
            }
        }

        private async void AddRemove_MouseDown(object sender, MouseButtonEventArgs e)
        {
           var configService = StaticService.Container.Resolve<Services.ConfigService>();
            var vkService = StaticService.Container.Resolve<VkService>();


            var config = await configService.GetConfig();

            if (Audio.OwnerId == config.UserId)
            {
                await vkService.AudioDeleteAsync(Audio.Id, Audio.OwnerId);
            }else
            {
                await vkService.AudioAddAsync(Audio.Id, Audio.OwnerId);

            }
        }

        private void Download_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var downloader = StaticService.Container.Resolve<DownloaderViewModel>();

            try
            {
                downloader.DownloadQueue.Add(Audio);
                downloader.StartDownloadingCommand.Execute(null);
            }catch(FileNotFoundException)
            {
                var navigation = StaticService.Container.Resolve<Services.NavigationService>();
                navigation.OpenMenuSection("downloads");
            }
        }

        private void Title_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Audio.Album != null)
            {
                Title.TextDecorations.Add(TextDecorations.Underline);
                this.Cursor = Cursors.Hand;
            }
        }

        private void Title_MouseLeave(object sender, MouseEventArgs e)
        {
            foreach (var dec in TextDecorations.Underline)
            {
                Title.TextDecorations.Remove(dec);
            }
            this.Cursor = Cursors.Arrow;
        }

        private async void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Audio.Album != null)
                {
                    var navigationService = StaticService.Container.Resolve<Services.NavigationService>();
                    navigationService.OpenExternalPage(new PlaylistView(Audio.Album.Id, Audio.Album.OwnerId, Audio.Album.AccessKey));
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
            }
            
        }

        private async void RecommendedAudio_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var notifications = StaticService.Container.Resolve<Services.NotificationsService>();

                notifications.Show("Уже ищем", "Сейчас мы найдем похожие треки, подождите");

                var vk = StaticService.Container.Resolve<VkService>();

                var items = await vk.GetRecommendationsAudio(Audio.OwnerId + "_" + Audio.Id);

                var navigation = StaticService.Container.Resolve<Services.NavigationService>();

                var ids = new List<string>();

                foreach (var audio in items.Response.Items)
                {
                    ids.Add(audio.OwnerId + "_" + audio.Id + "_" + audio.AccessKey);
                }

                var block = new MusicX.Core.Models.Block { Audios = items.Response.Items, AudiosIds = ids, DataType = "music_audios", Layout = new Layout() { Name = "list" } };
                var title = new MusicX.Core.Models.Block { DataType = "none", Layout = new Layout() { Name = "header", Title = $"Треки похожие на \"{Audio.Title}\"" } };

                var blocks = new List<Core.Models.Block>();
                blocks.Add(title);
                blocks.Add(block);

                navigation.OpenSection(items.Response.Section.Id);
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);
                var notifications = StaticService.Container.Resolve<Services.NotificationsService>();

                notifications.Show("Ошибка", "Мы не смогли найти подходящие треки");

            }


        }
        private void PlayNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                player.InsertToQueue(Audio, true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
        private void AddToQueue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                player.InsertToQueue(Audio, false);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void AddToPlaylist_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = StaticService.Container.Resolve<PlaylistSelectorModalViewModel>();
            var navigationService = StaticService.Container.Resolve<Services.NavigationService>();

            viewModel.PlaylistSelected += ViewModel_PlaylistSelected;

            navigationService.OpenModal<PlaylistSelectorModal>(viewModel);
        }

        private async void ViewModel_PlaylistSelected(Playlist playlist)
        {
            var vk = StaticService.Container.Resolve<VkService>();
            var notificationsService = StaticService.Container.Resolve<NotificationsService>();

            try
            {
                await vk.AddToPlaylistAsync(this.Audio, playlist.OwnerId, playlist.Id);

                notificationsService.Show("Трек добавлен", $"Трек '{this.Audio.Title}' добавлен в плейлист '{playlist.Title}'");


            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.Message);

                notificationsService.Show("Ошибка", "Произошла ошибка при добавлении трека в плейлист");
            }
        }
    }
}
