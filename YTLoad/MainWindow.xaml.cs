using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using YoutubeDLSharp;

namespace YTLoad
{
    public partial class MainWindow : Window
    {
        private YoutubeDL _ytdl;

        public MainWindow()
        {
            InitializeComponent();

            _ytdl = new YoutubeDL();
            _ytdl.YoutubeDLPath = "yt-dlp.exe";
            _ytdl.FFmpegPath = "ffmpeg.exe";
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Пожалуйста, введите ссылку.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DownloadButton.IsEnabled = false;
            StatusTextBlock.Text = "Проверка компонентов движка...";

            try
            {
                // скачивание ффмпега и yt-dlp при первом запуске
                if (!File.Exists("yt-dlp.exe") || !File.Exists("ffmpeg.exe"))
                {
                    StatusTextBlock.Text = "Загрузка необходимых модулей...";
                    await YoutubeDLSharp.Utils.DownloadBinaries();
                }

                StatusTextBlock.Text = "Получение информации о видео...";

                var videoData = await _ytdl.RunVideoDataFetch(url);
                if (videoData.Success)
                {
                    TitleTextBlock.Text = $"Название: {videoData.Data.Title}";
                    AuthorTextBlock.Text = $"Канал: {videoData.Data.Uploader}";
                    DurationTextBlock.Text = $"Просмотры: {videoData.Data.ViewCount}";
                }

                StatusTextBlock.Text = "Скачивание в максимальном качестве...";

                var progress = new Progress<DownloadProgress>(p =>
                {
                    double percentage = p.Progress * 100;
                    DownloadProgressBar.Value = percentage;
                    ProgressPercentageTextBlock.Text = $"{percentage:F0}%";
                    StatusTextBlock.Text = $"Скачивание: {p.State} ({p.DownloadSpeed})";
                });

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var options = new YoutubeDLSharp.Options.OptionSet()
                {
                    Output = Path.Combine(desktopPath, "%(title)s.%(ext)s") 
                };

                var result = await _ytdl.RunVideoDownload(
                    url,
                    progress: progress,
                    overrideOptions: options
                );

                if (result.Success)
                {
                    StatusTextBlock.Text = "Готово! Видео сохранено на Рабочий стол.";
                    MessageBox.Show("Видео успешно скачано!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusTextBlock.Text = "Ошибка скачивания.";
                    MessageBox.Show($"Ошибка: {string.Join("\n", result.ErrorOutput)}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Критическая ошибка.";
                MessageBox.Show($"Произошел сбой: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
                DownloadProgressBar.Value = 0;
                ProgressPercentageTextBlock.Text = "0%";
            }
        }
    }
}