﻿using BiliBili_Lib.Models.BiliBili.Video;
using BiliBili_Lib.Service;
using BiliBili_UWP.Components.Widgets;
using BiliBili_UWP.Dialogs;
using BiliBili_UWP.Models.Core;
using BiliBili_UWP.Models.UI.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BiliBili_UWP.Pages.Main
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ViewLaterPage : Page,IRefreshPage
    {
        private bool _isInit = false;
        public BiliViewModel biliVM = App.BiliViewModel;
        public AccountService _account = App.BiliViewModel._client.Account;
        private ObservableCollection<VideoDetail> ViewLaterCollection = new ObservableCollection<VideoDetail>();
        private bool _isViewLaterRequesting = false;
        private int _page = 1;
        private double _scrollOffset = 0;
        public ViewLaterPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            biliVM.IsLoginChanged += IsLoginChanged;
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.AppViewModel.CurrentPagePanel.ScrollToBottom = ScrollViewerBottomHandle;
            App.AppViewModel.CurrentPagePanel.ScrollChanged = ScrollViewerChanged;
            if (_isInit || e.NavigationMode == NavigationMode.Back)
            {
                return;
            }
            await Refresh();
            _isInit = true;
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.AppViewModel.CurrentPagePanel.ScrollToBottom = null;
            base.OnNavigatingFrom(e);
        }
        public async Task Refresh()
        {
            ViewLaterCollection.Clear();
            _page = 1;
            if (biliVM.IsLogin)
            {
                await LoadViewLater();
            }
        }
        private async Task LoadViewLater(bool isIncrease = false)
        {
            if (_isViewLaterRequesting)
                return;
            _isViewLaterRequesting = true;
            VideoLoadingBar.Visibility = Visibility.Visible;
            if (isIncrease)
                _page += 1;
            var data = await _account.GetViewLaterAsync(_page);
            if (data != null && data.Count > 0)
                data.ForEach(p => ViewLaterCollection.Add(p));
            HolderText.Visibility = ViewLaterCollection.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            VideoLoadingBar.Visibility = Visibility.Collapsed;
            _isViewLaterRequesting = false;
        }
        private async void ScrollViewerBottomHandle()
        {
            await LoadViewLater(true);
        }
        private async void IsLoginChanged(object sender, bool e)
        {
            await Refresh();
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (!biliVM.CheckAccoutStatus())
                return;
            var dialog = new ConfirmDialog("您确认清空稍后观看记录吗？");
            dialog.PrimaryButtonClick += async (_s, _e) =>
            {
                _e.Cancel = true;
                dialog.IsPrimaryButtonEnabled = false;
                dialog.PrimaryButtonText = "清空中...";
                bool reuslt = await _account.ClearViewLaterAsync();
                if (reuslt)
                {
                    new TipPopup("清空成功").ShowMessage();
                    ViewLaterCollection.Clear();
                    dialog.Hide();
                }
                else
                {
                    new TipPopup("清空失败").ShowError();
                }
                dialog.PrimaryButtonText = "确认";
                dialog.IsPrimaryButtonEnabled = true;
            };
            await dialog.ShowAsync();
        }

        private void HistoryVideoView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as VideoDetail;
            var ele = HistoryVideoView.ContainerFromItem(item);
            if (item.bangumi != null)
                App.AppViewModel.PlayBangumi(item.bangumi.ep_id, ele, true);
            else
                App.AppViewModel.PlayVideo(item.aid, ele);
        }

        private async void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as VideoDetail;
            bool result = await _account.DeleteViewLaterAsync(item.aid);
            if (result)
            {
                ViewLaterCollection.Remove(item);
            }
            else
            {
                new TipPopup("移出失败，请稍后重试").ShowError();
            }
        }
        private void ScrollViewerChanged()
        {
            double offset = App.AppViewModel.CurrentPagePanel.PageScrollViewer.VerticalOffset;
            _scrollOffset = offset;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_scrollOffset > 0)
            {
                App.AppViewModel.CurrentPagePanel.PageScrollViewer.ChangeView(0, _scrollOffset, 1);
            }
        }
    }
}