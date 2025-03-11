using System;
using System.Collections.Generic; 
using System.Collections.ObjectModel; 
using System.IO; 
using System.Linq; 
using System.Net.Http; 
using System.Threading; 
using System.Threading.Tasks; 
using System.Windows; 
using System.Windows.Controls; 
using Microsoft.Win32; 

namespace DownloadManager // Оголошення простору імен для менеджера завантажень
{
    public partial class MainWindow : Window // Оголошення класу головного вікна, що успадковує Window
    {
        private readonly HttpClient httpClient = new HttpClient(); // Ініціалізація HTTP-клієнта для завантаження файлів
        private readonly ObservableCollection<DownloadItem> downloads = new ObservableCollection<DownloadItem>(); // Колекція для відображення активних завантажень
        private readonly ObservableCollection<CompletedFile> completedFiles = new ObservableCollection<CompletedFile>(); // Колекція для завершених файлів
        private readonly Dictionary<string, DownloadTask> activeDownloads = new Dictionary<string, DownloadTask>(); // Словник для відстеження активних завантажень

        // Конструктор головного вікна
        public MainWindow()
        {
            InitializeComponent(); // Ініціалізація компонентів інтерфейсу
            lvDownloads.ItemsSource = downloads; // Прив’язка колекції завантажень до ListView
            lvCompleted.ItemsSource = completedFiles; // Прив’язка колекції завершених файлів до ListView
        }

        // Обробник натискання кнопки "Start" для початку завантаження
        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text.Trim(); // Отримання URL з текстового поля
            string savePath = txtSavePath.Text.Trim(); // Отримання шляху збереження з текстового поля
            int threadCount = int.Parse((cmbThreads.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "1"); // Отримання кількості потоків з комбобоксу
            var tags = txtTags.Text.Split(',').Select(t => t.Trim()).ToList(); // Розбиття тегів на список

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(savePath)) // Перевірка, чи введено URL і шлях
            {
                MessageBox.Show("Будь ласка, введіть URL та шлях збереження.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
                return; // Вихід із методу
            }

            // Додавання імені файлу, якщо шлях не містить його
            if (string.IsNullOrEmpty(Path.GetFileName(savePath)))
                savePath = Path.Combine(savePath, Path.GetFileName(new Uri(url).AbsolutePath) ?? "downloaded_file"); // Формування повного шляху

            // Перевірка прав доступу до шляху
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(savePath))) // Якщо директорія не існує
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath)); // Створення директорії
                using (var testStream = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // Тестове створення файлу
                { }
                File.Delete(savePath); // Видалення тестового файлу
            }
            catch (UnauthorizedAccessException) // Обробка помилки доступу
            {
                MessageBox.Show($"Немає прав доступу до {Path.GetDirectoryName(savePath)}. Використовуйте кнопку 'Choose Location' для вибору іншої папки.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
                return; // Вихід із методу
            }
            catch (Exception ex) // Обробка інших помилок
            {
                MessageBox.Show($"Помилка доступу до шляху збереження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
                return; // Вихід із методу
            }

            var download = new DownloadItem { Url = url, Progress = 0, Status = "Очікування" }; // Створення нового елемента завантаження
            downloads.Add(download); // Додавання до колекції завантажень

            var task = new DownloadTask(url, savePath, threadCount, tags, httpClient); // Створення задачі завантаження
            activeDownloads[url] = task; // Додавання задачі до активних завантажень

            try
            {
                await task.StartDownload((p, s) => Dispatcher.Invoke(() => UpdateProgress(url, p, s, savePath))).ConfigureAwait(false); // Запуск завантаження з оновленням прогресу
                activeDownloads.Remove(url); // Видалення задачі після завершення
            }
            catch (HttpRequestException ex) // Обробка помилок мережі
            {
                download.Status = $"Помилка мережі: {ex.Message}"; // Оновлення статусу
                activeDownloads.Remove(url); // Видалення задачі
            }
            catch (IOException ex) // Обробка помилок вводу/виводу
            {
                download.Status = $"Помилка введення/виведення: {ex.Message}"; // Оновлення статусу
                activeDownloads.Remove(url); // Видалення задачі
            }
            catch (Exception ex) // Обробка інших помилок
            {
                download.Status = $"Невідома помилка: {ex.Message}"; // Оновлення статусу
                activeDownloads.Remove(url); // Видалення задачі
            }
        }

        // Обробник кнопки "Choose Location" для вибору місця збереження
        private void BtnChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog // Створення діалогу вибору файлу
            {
                Title = "Виберіть папку, вибравши будь-який файл у ній (потім натисніть 'Відкрити')", // Заголовок діалогу
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop), // Початкова директорія
                CheckFileExists = false, // Вимкнення перевірки існування файлу
                FileName = "Виберіть цю папку", // Початкове ім’я файлу
                Filter = "Папка|*.folder" // Фільтр для відображення
            };
            if (dialog.ShowDialog() == true) // Якщо користувач вибрав папку
            {
                string selectedPath = Path.GetDirectoryName(dialog.FileName); // Отримання шляху до директорії
                string savePath = Path.Combine(selectedPath, Path.GetFileName(txtSavePath.Text) ?? "downloaded_file"); // Формування повного шляху
                txtSavePath.Text = savePath; // Оновлення текстового поля

                // Перевірка прав доступу
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(savePath))) // Якщо директорія не існує
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)); // Створення директорії
                    using (var testStream = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) // Тестове створення файлу
                    { }
                    File.Delete(savePath); // Видалення тестового файлу
                }
                catch (Exception ex) // Обробка помилок
                {
                    MessageBox.Show($"Неможливо записати до {savePath}: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
                }
            }
        }

        // Обробник кнопки "Pause" для призупинення/відновлення завантаження
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (lvDownloads.SelectedItem is DownloadItem selected && activeDownloads.TryGetValue(selected.Url, out var task)) // Якщо вибрано завантаження
            {
                task.Pause(); // Призупинення або відновлення задачі
                selected.Status = task.IsPaused ? "Призупинено" : "Завантаження"; // Оновлення статусу
            }
            else
            {
                MessageBox.Show("Жодного активного завантаження не вибрано або завантаження не розпочато.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning); // Повідомлення про помилку
            }
        }

        // Обробник кнопки "Stop" для зупинки завантаження
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (lvDownloads.SelectedItem is DownloadItem selected && activeDownloads.TryGetValue(selected.Url, out var task)) // Якщо вибрано завантаження
            {
                task.Stop(); // Зупинка задачі
                selected.Status = "Зупинено"; // Оновлення статусу
                downloads.Remove(selected); // Видалення з колекції
                activeDownloads.Remove(selected.Url); // Видалення з активних завантажень
            }
            else
            {
                MessageBox.Show("Жодного активного завантаження не вибрано або завантаження не розпочато.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning); // Повідомлення про помилку
            }
        }

        // Обробник кнопки "Search" для пошуку завершених файлів за тегами
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            var tags = txtSearchTags.Text.Split(',').Select(t => t.Trim()).ToList(); // Розбиття введених тегів на список
            if (string.IsNullOrEmpty(txtSearchTags.Text)) // Якщо поле пошуку порожнє
                lvCompleted.ItemsSource = completedFiles; // Показати всі завершені файли
            else
                lvCompleted.ItemsSource = completedFiles.Where(f => tags.All(t => f.Tags.Contains(t))); // Фільтрація за тегами
        }

        // Метод оновлення прогресу завантаження в інтерфейсі
        private void UpdateProgress(string url, double progress, string status, string savePath)
        {
            Dispatcher.Invoke(() => // Виконання в потоці UI
            {
                var download = downloads.FirstOrDefault(d => d.Url == url); // Пошук завантаження за URL
                if (download != null) // Якщо завантаження знайдено
                {
                    download.Progress = progress; // Оновлення прогресу
                    download.Status = status; // Оновлення статусу

                    if (progress >= 100) // Якщо завантаження завершено
                    {
                        downloads.Remove(download); // Видалення з активних завантажень
                        completedFiles.Add(new CompletedFile // Додавання до завершених файлів
                        {
                            Path = activeDownloads.ContainsKey(url) ? activeDownloads[url].SavePath : savePath, // Шлях до файлу
                            Tags = activeDownloads.ContainsKey(url) ? activeDownloads[url].Tags : new List<string>() // Теги файлу
                        });
                        if (activeDownloads.ContainsKey(url)) // Якщо є в активних
                            activeDownloads.Remove(url); // Видалення з активних завантажень
                    }
                }
            });
        }

        // Обробник кліку правою кнопкою миші на завершеному файлі
        private void LvCompleted_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvCompleted.SelectedItem is CompletedFile selected) // Якщо вибрано завершений файл
            {
                ContextMenu menu = new ContextMenu(); // Створення контекстного меню
                menu.Items.Add(new MenuItem { Header = "Delete", Command = new RelayCommand(() => DeleteFile(selected)) }); // Пункт "Видалити"
                menu.Items.Add(new MenuItem { Header = "Rename", Command = new RelayCommand(() => RenameFile(selected)) }); // Пункт "Перейменувати"
                menu.Items.Add(new MenuItem { Header = "Move", Command = new RelayCommand(() => MoveFile(selected)) }); // Пункт "Перемістити"
                menu.IsOpen = true; // Відкриття меню
            }
        }

        // Метод видалення завершеного файлу
        private void DeleteFile(CompletedFile file)
        {
            try
            {
                if (File.Exists(file.Path)) // Якщо файл існує
                {
                    File.Delete(file.Path); // Видалення файлу
                    completedFiles.Remove(file); // Видалення з колекції
                    MessageBox.Show("Файл видалено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information); // Повідомлення про успіх
                }
            }
            catch (Exception ex) // Обробка помилок
            {
                MessageBox.Show($"Помилка видалення файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
            }
        }

        // Метод перейменування завершеного файлу
        private void RenameFile(CompletedFile file)
        {
            try
            {
                string newName = ShowInputDialog("Введіть нову назву:", Path.GetFileName(file.Path)); // Запит нової назви
                if (!string.IsNullOrEmpty(newName)) // Якщо назва введена
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(file.Path), newName); // Новий шлях
                    if (File.Exists(newPath)) // Якщо файл із такою назвою вже існує
                    {
                        MessageBox.Show("Файл із такою назвою вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
                        return; // Вихід із методу
                    }
                    File.Move(file.Path, newPath); // Перейменування файлу
                    file.Path = newPath; // Оновлення шляху
                    MessageBox.Show("Файл перейменовано.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information); // Повідомлення про успіх
                }
            }
            catch (Exception ex) // Обробка помилок
            {
                MessageBox.Show($"Помилка перейменування файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
            }
        }

        // Метод переміщення завершеного файлу
        private void MoveFile(CompletedFile file)
        {
            try
            {
                string newPath = ShowInputDialog("Введіть новий шлях:", file.Path); // Запит нового шляху
                if (!string.IsNullOrEmpty(newPath)) // Якщо шлях введено
                {
                    if (File.Exists(newPath)) // Якщо файл із таким шляхом уже існує
                    {
                        MessageBox.Show("Файл із таким шляхом уже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
                        return; // Вихід із методу
                    }
                    File.Move(file.Path, newPath); // Переміщення файлу
                    file.Path = newPath; // Оновлення шляху
                    MessageBox.Show("Файл переміщено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information); // Повідомлення про успіх
                }
            }
            catch (Exception ex) // Обробка помилок
            {
                MessageBox.Show($"Помилка переміщення файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error); // Повідомлення про помилку
            }
        }

        // Метод відображення діалогового вікна для введення тексту
        private string ShowInputDialog(string prompt, string defaultValue)
        {
            Window dialog = new Window { Width = 300, Height = 150, Title = prompt }; // Створення вікна діалогу
            StackPanel panel = new StackPanel { Margin = new Thickness(10) }; // Панель для розміщення елементів
            TextBox input = new TextBox { Text = defaultValue, Margin = new Thickness(0, 0, 0, 10) }; // Текстове поле
            Button okButton = new Button { Content = "OK", Width = 75, HorizontalAlignment = HorizontalAlignment.Right }; // Кнопка "OK"
            okButton.Click += (s, e) => dialog.Close(); // Закриття діалогу при натисканні кнопки
            panel.Children.Add(new TextBlock { Text = prompt }); // Додавання підказки
            panel.Children.Add(input); // Додавання текстового поля
            panel.Children.Add(okButton); // Додавання кнопки
            dialog.Content = panel; // Встановлення вмісту діалогу
            dialog.ShowDialog(); // Відображення діалогу
            return input.Text; // Повернення введеного тексту
        }
    }

    public class DownloadTask // Клас для управління завантаженням
    {
        private readonly string url; // URL для завантаження
        public string SavePath { get; } // Шлях збереження файлу
        private readonly int threadCount; // Кількість потоків
        private readonly HttpClient httpClient; // HTTP-клієнт для запитів
        public List<string> Tags { get; } // Список тегів
        private CancellationTokenSource cts = new CancellationTokenSource(); // Токен для скасування
        public bool IsPaused { get; private set; } = false; // Статус паузи

        // Конструктор класу DownloadTask
        public DownloadTask(string url, string savePath, int threadCount, List<string> tags, HttpClient client)
        {
            this.url = url; // Ініціалізація URL
            SavePath = savePath; // Ініціалізація шляху
            this.threadCount = Math.Max(1, threadCount); // Встановлення кількості потоків (мін. 1)
            Tags = tags ?? new List<string>(); // Ініціалізація тегів
            this.httpClient = client; // Ініціалізація HTTP-клієнта
        }

        // Метод запуску асинхронного завантаження
        public async Task StartDownload(Action<double, string> progressCallback)
        {
            try
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false); // Отримання відповіді сервера
                response.EnsureSuccessStatusCode(); // Перевірка на успішність
                long totalBytes = response.Content.Headers.ContentLength ?? -1; // Отримання розміру файлу

                // Перевірка підтримки Range-запитів
                if (threadCount > 1 && !response.Headers.AcceptRanges.Any()) // Якщо сервер не підтримує багатопоточність
                {
                    progressCallback(0, "Server does not support multi-threaded download. Switching to single thread."); // Повідомлення про перехід на однопоточність
                    await DownloadSingleThread(response, totalBytes, progressCallback).ConfigureAwait(false); // Однопоточне завантаження
                }
                else if (totalBytes <= 0 || threadCount == 1) // Якщо розмір невідомий або обрано 1 потік
                {
                    await DownloadSingleThread(response, totalBytes, progressCallback).ConfigureAwait(false); // Однопоточне завантаження
                }
                else
                {
                    await DownloadMultiThread(totalBytes, progressCallback).ConfigureAwait(false); // Багатопоточне завантаження
                }

                if (cts.IsCancellationRequested) // Якщо завантаження скасовано
                    throw new OperationCanceledException(); // Виклик виключення
            }
            catch (Exception ex) // Обробка помилок
            {
                progressCallback(0, $"Failed: {ex.Message}"); // Повідомлення про помилку
            }
        }

        // Метод однопотокового завантаження
        private async Task DownloadSingleThread(HttpResponseMessage response, long totalBytes, Action<double, string> progressCallback)
        {
            using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) // Отримання потоку вмісту
            using (var fileStream = new FileStream(SavePath, FileMode.Create, FileAccess.Write)) // Створення файлу
            {
                var buffer = new byte[8192]; // Буфер для читання
                int bytesRead; // Кількість прочитаних байтів
                long downloadedBytes = 0; // Загальна кількість завантажених байтів

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) // Читання потоку
                {
                    while (IsPaused) await Task.Delay(100).ConfigureAwait(false); // Очікування при паузі
                    if (cts.IsCancellationRequested) break; // Переривання при скасуванні
                    await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false); // Запис у файл
                    downloadedBytes += bytesRead; // Оновлення кількості байтів
                    UpdateProgress(totalBytes, downloadedBytes, progressCallback); // Оновлення прогресу
                }
            }
        }

        // Метод багатопотокового завантаження
        private async Task DownloadMultiThread(long totalBytes, Action<double, string> progressCallback)
        {
            long partSize = totalBytes / threadCount; // Розмір частини для кожного потоку
            var tasks = new List<Task>(); // Список задач
            var tempFiles = new List<string>(); // Список тимчасових файлів
            long downloadedBytes = 0; // Загальна кількість завантажених байтів

            try
            {
                for (int i = 0; i < threadCount; i++) // Цикл для кожного потоку
                {
                    long start = i * partSize; // Початок частини
                    long end = (i == threadCount - 1) ? totalBytes - 1 : start + partSize - 1; // Кінець частини
                    string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp"); // Шлях до тимчасового файлу
                    tempFiles.Add(tempFile); // Додавання до списку

                    tasks.Add(DownloadPart(start, end, tempFile, (bytes) => Interlocked.Add(ref downloadedBytes, bytes))); // Додавання задачі завантаження частини
                }

                await Task.WhenAll(tasks).ConfigureAwait(false); // Очікування завершення всіх задач

                using (var fileStream = new FileStream(SavePath, FileMode.Create, FileAccess.Write)) // Створення кінцевого файлу
                {
                    foreach (var tempFile in tempFiles) // Об’єднання частин
                    {
                        while (IsPaused) await Task.Delay(100).ConfigureAwait(false); // Очікування при паузі
                        if (cts.IsCancellationRequested) break; // Переривання при скасуванні
                        using (var partStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read)) // Відкриття частини
                        {
                            await partStream.CopyToAsync(fileStream).ConfigureAwait(false); // Копіювання до основного файлу
                        }
                        File.Delete(tempFile); // Видалення тимчасового файлу
                    }
                }

                UpdateProgress(totalBytes, downloadedBytes, progressCallback); // Оновлення прогресу
            }
            finally
            {
                foreach (var tempFile in tempFiles) // Очищення тимчасових файлів
                    if (File.Exists(tempFile)) File.Delete(tempFile); // Видалення, якщо файл залишився
            }
        }

        // Метод завантаження частини файлу
        private async Task DownloadPart(long start, long end, string tempFile, Action<long> bytesDownloaded)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url); // Створення HTTP-запиту
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end); // Встановлення діапазону

                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false)) // Відправлення запиту
                {
                    response.EnsureSuccessStatusCode(); // Перевірка на успішність
                    using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) // Отримання потоку вмісту
                    using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write)) // Створення тимчасового файлу
                    {
                        var buffer = new byte[8192]; // Буфер для читання
                        int bytesRead; // Кількість прочитаних байтів
                        long totalRead = 0; // Загальна кількість прочитаних байтів

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) // Читання потоку
                        {
                            if (cts.IsCancellationRequested) break; // Переривання при скасуванні
                            await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false); // Запис у файл
                            totalRead += bytesRead; // Оновлення кількості байтів
                        }
                        bytesDownloaded(totalRead); // Повідомлення про завантажені байти
                    }
                }
            }
            catch (Exception ex) // Обробка помилок
            {
                Console.WriteLine($"Помилка в потоці: {ex.Message}"); // Виведення помилки (можна замінити логуванням)
            }
        }

        // Метод оновлення прогресу завантаження
        private void UpdateProgress(long totalBytes, long downloadedBytes, Action<double, string> progressCallback)
        {
            double progress = totalBytes > 0 ? (downloadedBytes * 100.0 / totalBytes) : 0; // Обчислення відсотка прогресу
            progressCallback(progress, progress >= 100 ? "Completed" : "Downloading"); // Виклик колбеку з прогресом і статусом
        }

        // Метод призупинення завантаження
        public void Pause()
        {
            IsPaused = !IsPaused; // Перемикання стану паузи
        }

        // Метод зупинки завантаження
        public void Stop()
        {
            cts.Cancel(); // Скасування задачі
            IsPaused = false; // Скидання стану паузи
        }
    }

    public class DownloadItem // Клас для відображення інформації про завантаження
    {
        public string Url { get; set; } // URL завантаження
        public double Progress { get; set; } // Прогрес у відсотках
        public string Status { get; set; } // Статус завантаження
    }

    public class CompletedFile // Клас для завершених файлів
    {
        public string Path { get; set; } // Шлях до файлу
        public List<string> Tags { get; set; } // Список тегів
        public string FileName => System.IO.Path.GetFileName(Path); // Властивість для отримання імені файлу
        public string TagsString => string.Join(",", Tags); // Властивість для відображення тегів як рядка
    }

    public class RelayCommand : System.Windows.Input.ICommand // Клас для команд у WPF
    {
        private readonly Action _execute; // Дія для виконання
        public event EventHandler CanExecuteChanged; // Подія зміни стану виконання
        public RelayCommand(Action execute) => _execute = execute; // Конструктор із переданою дією
        public bool CanExecute(object parameter) => true; // Завжди можна виконати
        public void Execute(object parameter) => _execute(); // Виконання дії
    }
}