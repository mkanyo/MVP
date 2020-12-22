﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using MVP.Extensions;
using MVP.Helpers;
using MVP.Models;
using MVP.Pages;
using MVP.Services.Interfaces;
using TinyMvvm;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Essentials;

namespace MVP.ViewModels
{
    public class ContributionsViewModel : BaseViewModel
    {
        const int pageSize = 20;

        public int ItemThreshold { get; set; } = 2;

        public ObservableCollection<Contribution> Contributions { get; set; } = new ObservableCollection<Contribution>();

        public IAsyncCommand OpenProfileCommand { get; set; }
        public IAsyncCommand RefreshDataCommand { get; set; }
        public IAsyncCommand LoadMoreCommand { get; set; }
        public IAsyncCommand<Contribution> OpenContributionCommand { get; set; }

        public bool IsLoadingMore { get; set; }

        public ContributionsViewModel(IAnalyticsService analyticsService, IDialogService dialogService, INavigationHelper navigationHelper)
            : base(analyticsService, dialogService, navigationHelper)
        {
            OpenContributionCommand = new AsyncCommand<Contribution>((Contribution c) => OpenContribution(c));
            SecondaryCommand = new AsyncCommand(() => OpenAddContribution());
            RefreshDataCommand = new AsyncCommand(() => RefreshContributions());
            LoadMoreCommand = new AsyncCommand(() => LoadMore());
        }

        public async override Task Initialize()
        {
            await base.Initialize();
            RefreshContributions().SafeFireAndForget();
        }

        async Task RefreshContributions()
        {
            Contributions.Clear();
            ItemThreshold = 2;

            try
            {
                State = LayoutState.Loading;

                Contributions.Clear();

                var contributionsList = await MvpApiService.GetContributionsAsync(0, pageSize).ConfigureAwait(false);

                if (contributionsList == null)
                    return;

                Contributions = new ObservableCollection<Contribution>(contributionsList.Contributions.OrderByDescending(x => x.StartDate).ToList());
            }
            finally
            {
                State = LayoutState.None;
            }
        }

        async Task LoadMore()
        {
            if (IsLoadingMore)
                return;

            try
            {
                IsLoadingMore = true;

                var contributionsList = await MvpApiService.GetContributionsAsync(Contributions.Count, pageSize).ConfigureAwait(false);

                foreach (var item in contributionsList.Contributions.OrderByDescending(x => x.StartDate))
                {
                    Contributions.Add(item);
                }

                // If we've reached the end, change the threshold.
                if (!contributionsList.Contributions.Any())
                {
                    ItemThreshold = -1;
                    return;
                }
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        async Task OpenAddContribution(Contribution prefilledData = null)
            => await NavigationHelper.OpenModalAsync(nameof(ContributionFormPage), prefilledData, true).ConfigureAwait(false);

        async Task OpenContribution(Contribution contribution)
            => await NavigationHelper.NavigateToAsync(nameof(ContributionDetailsPage), contribution).ConfigureAwait(false);
    }
}
