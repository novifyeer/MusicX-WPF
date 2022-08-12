﻿using MusicX.ViewModels.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace MusicX.Views.Modals
{
    /// <summary>
    /// Логика взаимодействия для PlaylistSelectorModal.xaml
    /// </summary>
    public partial class PlaylistSelectorModal : Page
    {

        public PlaylistSelectorModal(PlaylistSelectorModalViewModel vm)
        {
            this.DataContext = vm;
            this.Loaded += PlaylistSelectorModal_Loaded;
            InitializeComponent();
        }

        private async void PlaylistSelectorModal_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.DataContext is PlaylistSelectorModalViewModel vm)
            {
                await vm.LoadPlaylistsAsync();
            }
        }
    }
}
