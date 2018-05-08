using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using static Windows.Storage.FileIO;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using AngleSharp;
using AngleSharp.Css.Values;
using AngleSharp.Dom;
using AngleSharp.Extensions;

namespace Email_parser
{
    public sealed partial class MainPage : Page
    {
        private List<string> emails = new List<string>();
        public string BaseUrl { get; } = "https://www.yandex.ru/search/?text=";

        public MainPage()
        {
            InitializeComponent();
        }


        private async void BtnUploadEmail_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var file = await GetFileWithEmails();
            await GetEmailsFromFile(file);
            LabelEmailsPath.Text = file?.Path ?? "Файл не найден.";
            LabelEmailsCount.Text = emails.Count.ToString();
        }

        private async void BtnSearch_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (TBSearchWords.Text.Equals(string.Empty))
            {
                ShowDialog("Введите поисковый запрос");
            }
            else
            {
                var pageList = await GetYaLinks();
                var siteList = new List<string>
                {
                    "http://regexstorm.net/",
                    "https://cloudpayments.ru/",
                    "https://xml.yandex.ru/test/",
                    "https://www.1metallobaza.ru/catalog/truba"
                };
                Progress.Maximum = siteList.Count;
                var siteEmails = new List<string>();
                foreach (var link in siteList)
                {
                    siteEmails.AddRange(ParsePage(await GetHtml(link)));
                    Progress.Value += 1;
                }
            }
        }

        //Сбор ссылок на страницы Яндекса, которые нужно парсить
        async Task<List<string>> GetYaLinks(int maxPage = 0)
        {
            var pageList = new List<string>();
            if (maxPage == 0)
            {
                int pageCounter = 1;
                while (CheckYaPage(await GetHtml(BaseUrl + TBSearchWords.Text + $"&p={pageCounter}")))
                {
                    pageList.Add(BaseUrl + TBSearchWords.Text + $"&p={pageCounter}");
                    pageCounter += 1;
                }
            }
            else
            {
                for (var i = 0; i < maxPage; i++)
                {
                    pageList.Add(BaseUrl + TBSearchWords.Text + $"&p={i}");
                }
            }

            return pageList;
        }

        //Сбор почт со страницы
        private List<string> ParsePage(IDocument htmlDocument)
        {
            var emailRegex = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*",
                RegexOptions.IgnoreCase);

            var emailMatches = emailRegex.Matches(htmlDocument.Body.OuterHtml+ htmlDocument.Body.Html());

            var siteEmails = new List<string>();
            Log($"Парсинг сайта: {htmlDocument.BaseUri}");
            foreach (Match emailMatch in emailMatches)
            {
                siteEmails.Add(emailMatch.Value);
                TBLog.Text += $"{emailMatch.Value}";
            }
            Log($"Найдено Email`ов {siteEmails.Count}");
            return siteEmails;

        }

        //Получение Html-кода страницы по ссылке
        private async Task<IDocument> GetHtml(string url)
        {
            var config = new Configuration();
            try
            {
                IDocument htmlDocument = await BrowsingContext.New(config).OpenAsync(url);
                return htmlDocument;
            }
            catch
            {
                ShowDialog($"Ошибка. Не удалось открыть страницу: {url}");
                return null;
            }
        }
        //Получение всех почт из текстового файла
        private async Task GetEmailsFromFile(IStorageFile file)
        {
            Log("Получение почт...\n");
            var textFromFile = await ReadTextAsync(file);
            emails = textFromFile.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            TBLog.Text += "Найдены почты:\n";
            foreach (var email in emails)
            {
                TBLog.Text += email + "\n";
            }
            TBLog.Text += $"Всего {emails.Count}";
        }

        //Получение пути к файлу с почтами
        private async Task<StorageFile> GetFileWithEmails()
        {
            Log("Выбор файла...\n");
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".txt");
            StorageFile file = await picker.PickSingleFileAsync();
            Log($"Выбран файл: {file?.Name}");
            return file;
        }

        //Вывод модального окна с текстом
        async void ShowDialog(string text)
        {
            var messageDialog = new MessageDialog(text);
            await messageDialog.ShowAsync();
            Log($"Ошибка: {text}");
        }

        //Проверка, есть ли на поисковой странице ссылки
        bool CheckYaPage(IParentNode htmlDocument) => htmlDocument.QuerySelector(".serp-item")?.InnerHtml != null;
        
        //Запись в лог
        void Log(string text) => TBLog.Text += $"\n-----\n{text}\n";
    }
}
