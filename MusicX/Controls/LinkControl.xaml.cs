﻿using DryIoc;
using MusicX.Core.Models;
using MusicX.Core.Services;
using MusicX.Services;
using MusicX.Views;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicX.Controls
{
    /// <summary>
    /// Логика взаимодействия для LinkControl.xaml
    /// </summary>
    public partial class LinkControl : UserControl
    {
        private readonly VkService vkService;
        private readonly Services.NavigationService navigationService;
        private readonly Logger logger;

        public LinkControl()
        {
            InitializeComponent();
            vkService = StaticService.Container.Resolve<VkService>();
            navigationService = StaticService.Container.Resolve<Services.NavigationService>();
            logger = StaticService.Container.Resolve<Logger>();

        }

        public static readonly DependencyProperty LinkProperty =
          DependencyProperty.Register("Link", typeof(Link), typeof(LinkControl), new PropertyMetadata(new Link()));

        public Link Link
        {
            get { return (Link)GetValue(LinkProperty); }
            set
            {
                SetValue(LinkProperty, value);
            }
        }

        public bool FullLink{ get; set; }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                if(Link.Url != null)
                {
                    RectanglePlaceholder.Visibility = Visibility.Collapsed;
                    IconPlaceholder.Visibility = Visibility.Collapsed;
                }

                LinkText.Text = Link.Title;
                if(Link.Meta.ContentType == null)
                {
                    this.FullLink = true;
                    this.Width = 200;
                    this.Height = 80;
                }

                if (Link.Meta.ContentType == "group" || Link.Meta.ContentType == "user")
                {
                    if (Link.Image != null) LinkImage.ImageSource = new BitmapImage(new Uri(Link.Image[1].Url));

                }else
                {
                    if (Link.Image != null) LinkImage.ImageSource = new BitmapImage(new Uri(Link.Image[0].Url));

                }

                if (FullLink)
                {
                    StackPanelLink.Orientation = Orientation.Horizontal;
                    RectangleLink.Height = 50;
                    RectangleLink.Width = 50;
                    RectanglePlaceholder.Width = 50;
                    RectanglePlaceholder.Height = 50;
                    LinkText.Margin = new Thickness(10, 0, 0, 0);
                    LinkText.VerticalAlignment = VerticalAlignment.Center;

                    if (Link.Meta.ContentType == "group" || Link.Meta.ContentType == "user")
                    {
                        Card.Icon = Wpf.Ui.Common.SymbolRegular.Link48;
                    }
                }
            }catch (Exception ex)
            {
                logger.Error("Fail load link control");
                logger.Error(ex, ex.Message);
            }
            
        }

        private async void CardAction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Link.Meta.ContentType == null)
                {
                    var match = Regex.Match(Link.Url, "https://vk.com/podcasts\\?category=[0-9]+$");

                    if (match.Success)
                    {
                        //var podcasts = await vkService.GetPodcastsAsync(Link.Url);
                        //await navigationService.OpenSection(podcasts.Catalog.DefaultSection, true);

                        return;

                    }
                    var music = await vkService.GetAudioCatalogAsync(Link.Url);
                    navigationService.OpenSection(music.Catalog.DefaultSection);

                    return;
                }

                if (Link.Meta.ContentType == "artist")
                {
                    var url = new Uri(Link.Url);

                    navigationService.OpenSection(url.Segments.LastOrDefault(), SectionType.Artist);
                }

                if (Link.Meta.ContentType == "group" || Link.Meta.ContentType == "user")
                {
                    var match = Regex.Match(Link.Url, "https://vk.com/audios[0-9]+$");
                    if(match.Success)
                    {
                        var music = await vkService.GetAudioCatalogAsync(Link.Url);

                        navigationService.OpenSection(music.Catalog.DefaultSection);

                        return;
                    }

                  
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Link.Url,
                        UseShellExecute = true
                    });
                }

                if (Link.Meta.ContentType == "curator")
                {

                    var curator = await vkService.GetAudioCuratorAsync(Link.Meta.TrackCode, Link.Url);

                    navigationService.OpenSection(curator.Catalog.DefaultSection);

                }
            }catch(Exception ex)
            {
                logger.Error("Fail click action in link control");
                logger.Error(ex, ex.Message);
            }
           
        }
    }
}
