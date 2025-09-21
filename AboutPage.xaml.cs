using Microsoft.Maui.Controls;
using System;

namespace BuraunzaApp
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 访问官方网站按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private async void OnVisitWebsiteClicked(object sender, EventArgs e)
        {
            try
            {
                // 这里可以替换为实际的官方网站URL
                var websiteUrl = "https://hub.imikufans.cn";
                await Launcher.OpenAsync(websiteUrl);
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", "无法打开网站: " + ex.Message, "确定");
            }
        }

        /// <summary>
        /// 查看源码按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private async void OnViewSourceCodeClicked(object sender, EventArgs e)
        {
            try
            {
                // 这里可以替换为实际的源码仓库URL
                var sourceCodeUrl = "https://github.com/iMikufans/BuraunzaApp";
                await Launcher.OpenAsync(sourceCodeUrl);
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", "无法打开源码仓库: " + ex.Message, "确定");
            }
        }
    }
}