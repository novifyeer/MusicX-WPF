﻿using AsyncAwaitBestPractices.MVVM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MusicX.Core.Services;
using MusicX.Services;
using MusicX.Shared.ListenTogether.Radio;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using Wpf.Ui;
using NavigationService = MusicX.Services.NavigationService;

namespace MusicX.ViewModels.Modals
{
    public class CreateUserRadioModalViewModel : BaseViewModel
    {
        public string TitleRadio { get; set; }

        public string DescriptionRadio { get; set; }

        public ICommand SelectRadioCoverCommand { get; set; }

        public ICommand CreateRadioCommand { get; set; }

        public string? CoverPath { get; set; }

        public bool IsLoading { get; set; }

        public event EventHandler<Station>? StationCreated;

        public CreateUserRadioModalViewModel()
        {
            SelectRadioCoverCommand = new AsyncCommand(SelectRadioCover);
            CreateRadioCommand = new AsyncCommand(CreateRadio);
        }

        private async Task SelectRadioCover()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения ( *.jpg)|*.jpg";
            if (openFileDialog.ShowDialog() == true)
            {
                CoverPath = openFileDialog.FileName;
            }
        }

        private async Task CreateRadio()
        {
            var radioService = StaticService.Container.GetRequiredService<UserRadioService>();
            var listenTogetherService = StaticService.Container.GetRequiredService<ListenTogetherService>();
            var configService = StaticService.Container.GetRequiredService<ConfigService>();
            var snackbarService = StaticService.Container.GetRequiredService<ISnackbarService>();
            var logger = StaticService.Container.GetRequiredService<Logger>();
            var navigationService = StaticService.Container.GetRequiredService<NavigationService>();
            var usersCategory = StaticService.Container.GetRequiredService<IUsersCategory>();

            if(string.IsNullOrEmpty(TitleRadio))
            {
                snackbarService.Show("Ошибка", "Вы не заполнили название");
                return;
            }

            if (string.IsNullOrEmpty(DescriptionRadio))
            {
                snackbarService.Show("Ошибка", "Вы не заполнили описание");
                return;
            }

            if(listenTogetherService.IsConnectedToServer && listenTogetherService.PlayerMode == Core.Models.PlayerMode.Listener)
            {
                snackbarService.Show("Ошибка", "Сначала Вам необходимо отключиться от совместного просшуливания");
                return;
            }

            try
            {
                IsLoading = true;

                var config = await configService.GetConfig();

                var session = string.Empty;

                if (listenTogetherService.IsConnectedToServer && listenTogetherService.PlayerMode == Core.Models.PlayerMode.Owner)
                {
                    session = listenTogetherService.SessionId;
                }else
                {
                    session = await listenTogetherService.StartSessionAsync(config.UserId);

                }

                var user = (await usersCategory.GetAsync(new[] { config.UserId }, ProfileFields.Photo100))[0];

                var coverUrl = "https://as2.ftcdn.net/v2/jpg/02/87/95/77/1000_F_287957705_kVUIWM8TnTbavhGX9JTEAQLGQo6fVrc5.jpg";
                if (!string.IsNullOrEmpty(CoverPath) && File.Exists(CoverPath))
                    coverUrl = await radioService.UploadCoverAsync(CoverPath);

                var station = await radioService.CreateStationAsync(session,
                    TitleRadio,
                    coverUrl, 
                    DescriptionRadio,
                    config.UserId, 
                    config.UserName, user.Photo100.ToString());

                navigationService.CloseModal();

                IsLoading = false;

                StationCreated?.Invoke(this, station);
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                snackbarService.Show("Ошибка", "Мы не смогли создать радиостанцию");
            }
        }
    }
}
