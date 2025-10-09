using DiagnosticNP.Models;
using DiagnosticNP.Models.Vibrometer;
using DiagnosticNP.Repositories;
using DiagnosticNP.Services;
using DiagnosticNP.Services.Bluetooth;
using DiagnosticNP.Services.Nfc;
using DiagnosticNP.Services.Utils;
using DiagnosticNP.Views;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace DiagnosticNP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly INfcService _nfcService;
        private readonly ControlPointRepository _controlPointRepository;
        private readonly MeasurementRepository _measurementRepository;
        private SQLiteAsyncConnection _database;
        private ObservableCollection<ControlPoint> _controlPoints;
        private ControlPoint _selectedControlPoint;
        private string _nfcFilter;
        private bool _isPollingVibrometer;
        private string _statusMessage;
        private int _pendingMeasurementsCount;

        public MainViewModel()
        {
            _apiService = new MockApiService();
            _nfcService = new NfcService();

            _database = DependencyService.Get<IDatabaseService>().GetConnection();
            _controlPointRepository = new ControlPointRepository(_database);
            _measurementRepository = new MeasurementRepository(_database);

            ControlPoints = new ObservableCollection<ControlPoint>();
            Measurements = new ObservableCollection<Measurement>();

            InitializeCommands();
            InitializeNfc();
            InitializeVibrometer();

            _ = LoadControlPointsFromDatabase();
            _ = LoadPendingMeasurements();
        }

        public ObservableCollection<ControlPoint> ControlPoints
        {
            get => _controlPoints;
            set => SetProperty(ref _controlPoints, value);
        }

        public ObservableCollection<Measurement> Measurements { get; private set; }

        public ControlPoint SelectedControlPoint
        {
            get => _selectedControlPoint;
            set
            {
                if (SetProperty(ref _selectedControlPoint, value) && value != null)
                {
                    OnPropertyChanged(nameof(CanTakeMeasurement));
                    System.Diagnostics.Debug.WriteLine($"Выбрана точка: {value.FullPath}");
                }
            }
        }

        public string NfcFilter
        {
            get => _nfcFilter;
            set
            {
                if (SetProperty(ref _nfcFilter, value))
                {
                    FilterControlPoints();
                }
            }
        }

        public bool IsPollingVibrometer
        {
            get => _isPollingVibrometer;
            set => SetProperty(ref _isPollingVibrometer, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int PendingMeasurementsCount
        {
            get => _pendingMeasurementsCount;
            set => SetProperty(ref _pendingMeasurementsCount, value);
        }

        public bool CanTakeMeasurement => SelectedControlPoint?.HasMeasurements == true;

        public ICommand LoadControlPointsCommand { get; private set; }
        public ICommand UploadMeasurementsCommand { get; private set; }
        public ICommand TakeMeasurementCommand { get; private set; }
        public ICommand ClearMeasurementsCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ClearNfcFilterCommand { get; private set; }

        private void InitializeCommands()
        {
            LoadControlPointsCommand = new Command(async () => await LoadControlPointsFromApi());
            UploadMeasurementsCommand = new Command(async () => await UploadMeasurements());
            TakeMeasurementCommand = new Command(async () => await TakeMeasurement(), () => CanTakeMeasurement);
            ClearMeasurementsCommand = new Command(async () => await ClearMeasurements());
            RefreshCommand = new Command(async () => await RefreshData());
            ClearNfcFilterCommand = new Command(() => ClearNfcFilter());

            // Обновляем доступность команды TakeMeasurement при изменении SelectedControlPoint
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedControlPoint))
                {
                    (TakeMeasurementCommand as Command)?.ChangeCanExecute();
                }
            };
        }

        private async void InitializeNfc()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ИНИЦИАЛИЗАЦИЯ NFC ===");

                // Проверяем доступность NFC
                var isAvailable = await _nfcService.IsAvailableAsync();
                System.Diagnostics.Debug.WriteLine($"NFC доступен: {isAvailable}");

                if (!isAvailable)
                {
                    StatusMessage = "NFC не поддерживается на этом устройстве";
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "NFC не поддерживается на этом устройстве", "OK");
                    return;
                }

                // Проверяем включен ли NFC
                var isEnabled = await _nfcService.IsEnabledAsync();
                System.Diagnostics.Debug.WriteLine($"NFC включен: {isEnabled}");

                if (!isEnabled)
                {
                    StatusMessage = "NFC отключен";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "NFC отключен. Включите NFC в настройках устройства.", "OK");
                    return;
                }

                // Подписываемся на события
                _nfcService.TagScanned += OnTagScanned;

                // Запускаем прослушивание
                _nfcService.StartListening();

                StatusMessage = "NFC сканер запущен";
                System.Diagnostics.Debug.WriteLine("NFC инициализирован и запущен");

                // Периодическая проверка статуса NFC
                Device.StartTimer(TimeSpan.FromSeconds(10), () =>
                {
                    CheckNfcStatus();
                    return true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации NFC: {ex.Message}");
                StatusMessage = "Ошибка инициализации NFC";
            }
        }

        private async void CheckNfcStatus()
        {
            try
            {
                var isAvailable = await _nfcService.IsAvailableAsync();
                var isEnabled = await _nfcService.IsEnabledAsync();

                if (!isAvailable)
                {
                    StatusMessage = "NFC недоступен";
                }
                else if (!isEnabled)
                {
                    StatusMessage = "NFC отключен";
                }
                else
                {
                    StatusMessage = "Готов к сканированию NFC";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка проверки статуса NFC: {ex.Message}");
            }
        }

        private async void OnTagScanned(object sender, string nfcData)
        {
            if (string.IsNullOrWhiteSpace(nfcData))
            {
                System.Diagnostics.Debug.WriteLine("NFC: Получены пустые данные");
                return;
            }

            await Device.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"NFC: Обработка данных метки: {nfcData}");

                    // Очищаем данные от лишних символов
                    var cleanData = nfcData.Trim();

                    // Убираем возможные префиксы
                    if (cleanData.StartsWith("en|"))
                        cleanData = cleanData.Substring(3);
                    else if (cleanData.StartsWith("ru|"))
                        cleanData = cleanData.Substring(3);

                    // Нормализуем разделители путей
                    cleanData = cleanData.Replace('/', '\\');

                    System.Diagnostics.Debug.WriteLine($"NFC: Нормализованные данные: {cleanData}");

                    NfcFilter = cleanData;
                    StatusMessage = $"NFC метка: {cleanData}";

                    // Автоматически фильтруем точки контроля
                    FilterControlPoints();

                    // Показываем уведомление после фильтрации
                    var targetPoint = FindControlPointByPartialPath(ControlPoints, cleanData);
                    if (targetPoint != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("NFC Метка",
                            $"Просканирована метка:\n{cleanData}\n\nНайдена точка:\n{targetPoint.FullPath}", "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("NFC Метка",
                            $"Просканирована метка:\n{cleanData}\n\nТочка не найдена в базе данных", "OK");
                    }

                    System.Diagnostics.Debug.WriteLine($"NFC: Метка обработана: {cleanData}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка обработки NFC метки: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        $"Ошибка обработки NFC метки: {ex.Message}", "OK");
                }
            });
        }

        private async void CheckNfcAvailability()
        {
            try
            {
                var isAvailable = await _nfcService.IsAvailableAsync();
                var isEnabled = await _nfcService.IsEnabledAsync();

                if (!isAvailable || !isEnabled)
                {
                    StatusMessage = "NFC отключен или недоступен";
                }
                else
                {
                    StatusMessage = "Готов к работе";
                }
            }
            catch
            {
                StatusMessage = "Ошибка проверки NFC";
            }
        }

        private void InitializeVibrometer()
        {
            try
            {
                if (!BluetoothController.LeScanner.IsRunning)
                {
                    BluetoothController.LeScanner.Restart();
                }

                BluetoothController.LeScanner.NewData += OnVibrometerDataReceived;
                StatusMessage = "BLE сканер запущен";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации BLE: {ex.Message}");
                StatusMessage = "BLE недоступен";
            }
        }

        

        private void OnVibrometerDataReceived(object sender, BleDataEventArgs e)
        {
            // Обработка advertising данных виброметра
            if (IsPollingVibrometer) return; // Не обрабатываем во время активного опроса

            try
            {
                var data = e.Data.Data.BytesToStruct<ViPenAdvertising>();
                System.Diagnostics.Debug.WriteLine($"Advertising данные: V={data.Velocity}, A={data.Acceleration}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обработки advertising данных: {ex.Message}");
            }
        }

        private void FilterControlPoints()
        {
            if (string.IsNullOrEmpty(NfcFilter))
            {
                // Сбрасываем фильтр - показываем все развернутые узлы
                foreach (var point in ControlPoints)
                {
                    point.IsExpanded = true;
                }
                return;
            }

            System.Diagnostics.Debug.WriteLine($"=== ПОИСК ПО NFC МЕТКЕ: {NfcFilter} ===");

            // Ищем точку, соответствующую NFC метке
            var targetPoint = FindControlPointByPartialPath(ControlPoints, NfcFilter);

            if (targetPoint != null)
            {
                System.Diagnostics.Debug.WriteLine($"Найдена точка: {targetPoint.FullPath}");

                // Сворачиваем все узлы
                CollapseAllNodes(ControlPoints);

                // Раскрываем путь к целевой точке
                ExpandPathToPoint(targetPoint);

                // Выделяем целевую точку
                SelectedControlPoint = targetPoint;

                StatusMessage = $"Найдена точка: {targetPoint.Name}";

                // Прокручиваем TreeView к выбранному элементу
                ScrollToSelectedItem();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Точка не найдена для метки: {NfcFilter}");
                StatusMessage = $"Точка не найдена для метки: {NfcFilter}";

                // Показываем все дерево, если точка не найдена
                foreach (var point in ControlPoints)
                {
                    point.IsExpanded = true;
                }

                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        $"Контрольная точка для метки '{NfcFilter}' не найдена\n\n" +
                        $"Доступные пути:\n{GetAvailablePaths()}", "OK");
                });
            }
        }

        // Новый метод для поиска по частичному пути
        private ControlPoint FindControlPointByPartialPath(IEnumerable<ControlPoint> points, string searchPath)
        {
            if (points == null || string.IsNullOrEmpty(searchPath))
                return null;

            // Нормализуем путь поиска
            var normalizedSearchPath = searchPath.Trim().ToLower();

            System.Diagnostics.Debug.WriteLine($"Поиск пути: '{normalizedSearchPath}'");

            foreach (var point in points)
            {
                // Проверяем полный путь
                if (!string.IsNullOrEmpty(point.FullPath))
                {
                    var normalizedFullPath = point.FullPath.ToLower();
                    System.Diagnostics.Debug.WriteLine($"Сравниваем с: '{normalizedFullPath}'");

                    // Ищем частичное совпадение
                    if (normalizedFullPath.Contains(normalizedSearchPath) ||
                        normalizedSearchPath.Contains(normalizedFullPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Найдено совпадение: {point.FullPath}");
                        return point;
                    }

                    // Проверяем совпадение по последним сегментам пути
                    var searchSegments = normalizedSearchPath.Split('\\');
                    var fullPathSegments = normalizedFullPath.Split('\\');

                    if (searchSegments.Length > 0 && fullPathSegments.Length >= searchSegments.Length)
                    {
                        bool match = true;
                        for (int i = 0; i < searchSegments.Length; i++)
                        {
                            var searchSegment = searchSegments[searchSegments.Length - 1 - i].Trim();
                            var fullPathSegment = fullPathSegments[fullPathSegments.Length - 1 - i].Trim();

                            if (searchSegment != fullPathSegment)
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            System.Diagnostics.Debug.WriteLine($"Найдено совпадение по сегментам: {point.FullPath}");
                            return point;
                        }
                    }
                }

                // Рекурсивно ищем в детях
                var found = FindControlPointByPartialPath(point.Children, searchPath);
                if (found != null)
                    return found;
            }

            return null;
        }

        // Метод для получения списка доступных путей (для отладки)
        private string GetAvailablePaths()
        {
            var paths = new List<string>();
            CollectPaths(ControlPoints, paths);
            return string.Join("\n", paths.Take(10)); // Показываем первые 10 путей
        }

        private void CollectPaths(IEnumerable<ControlPoint> points, List<string> paths)
        {
            foreach (var point in points)
            {
                if (!string.IsNullOrEmpty(point.FullPath))
                {
                    paths.Add(point.FullPath);
                }
                CollectPaths(point.Children, paths);
            }
        }

        // Метод для прокрутки к выбранному элементу
        private void ScrollToSelectedItem()
        {
            // Для Syncfusion TreeView может потребоваться специальный метод прокрутки
            // Пока просто устанавливаем выделение
            if (SelectedControlPoint != null)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Здесь может быть логика прокрутки TreeView
                    // В зависимости от реализации TreeView
                    System.Diagnostics.Debug.WriteLine($"Выбрана точка: {SelectedControlPoint.FullPath}");
                });
            }
        }

        private void CollapseAllNodes(IEnumerable<ControlPoint> points)
        {
            foreach (var point in points)
            {
                point.IsExpanded = false;
                CollapseAllNodes(point.Children);
            }
        }

        private void ExpandPathToPoint(ControlPoint point)
        {
            if (point == null) return;

            // Раскрываем все родительские узлы
            var current = point;
            while (current != null)
            {
                current.IsExpanded = true;
                // Здесь нужно найти родительский узел - для простоты раскрываем все
                break;
            }
        }

        private void ClearNfcFilter()
        {
            NfcFilter = string.Empty;
            StatusMessage = "Фильтр сброшен";
        }

        private async Task SaveControlPointsRecursive(IEnumerable<ControlPoint> points)
        {
            foreach (var point in points)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Сохранение: {point.Name} (ID: {point.Id}, ParentID: {point.ParentId})");

                    // Используем InsertOrReplace для избежания конфликтов
                    var result = await _database.InsertOrReplaceAsync(point);
                    System.Diagnostics.Debug.WriteLine($"Результат сохранения: {result}");

                    // Сохраняем детей
                    if (point.Children != null && point.Children.Any())
                    {
                        await SaveControlPointsRecursive(point.Children);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения {point.Name}: {ex.Message}");
                }
            }
        }

        private async Task LoadControlPointsFromApi()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Загрузка контрольных точек...";

            try
            {
                System.Diagnostics.Debug.WriteLine("=== НАЧАЛО ЗАГРУЗКИ ИЗ API ===");

                var points = await _apiService.GetControlPointsAsync();
                System.Diagnostics.Debug.WriteLine($"Получено из API: {points?.Count ?? 0} точек");

                if (points != null && points.Any())
                {
                    // Удаляем старые данные
                    System.Diagnostics.Debug.WriteLine("Удаление старых данных...");
                    await _database.DeleteAllAsync<ControlPoint>();

                    // Сохраняем новые данные рекурсивно
                    System.Diagnostics.Debug.WriteLine("Сохранение новых данных...");
                    await SaveControlPointsRecursive(points);

                    // Загружаем из БД
                    System.Diagnostics.Debug.WriteLine("Загрузка из БД...");
                    await LoadControlPointsFromDatabase();

                    StatusMessage = $"Загружено {CountAllNodes(points)} точек";
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Контрольные точки успешно загружены и сохранены", "OK");
                }
                else
                {
                    StatusMessage = "Нет данных для загрузки";
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "Нет данных для загрузки", "OK");
                }

                System.Diagnostics.Debug.WriteLine("=== ЗАВЕРШЕНИЕ ЗАГРУЗКИ ИЗ API ===");
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка загрузки";
                System.Diagnostics.Debug.WriteLine($"ОШИБКА загрузки из API: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка загрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadControlPointsFromDatabase()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ЗАГРУЗКА ИЗ БАЗЫ ДАННЫХ ===");

                // Используем прямой запрос к БД для проверки
                var allPoints = await _database.Table<ControlPoint>().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"Всего записей в таблице ControlPoints: {allPoints.Count}");

                foreach (var point in allPoints)
                {
                    System.Diagnostics.Debug.WriteLine($"Точка: {point.Name} (ID: {point.Id}, ParentID: {point.ParentId})");
                }

                // Загружаем дерево через репозиторий
                var tree = await _controlPointRepository.GetTreeAsync();

                ControlPoints.Clear();
                foreach (var root in tree)
                {
                    ControlPoints.Add(root);
                }

                if (!ControlPoints.Any())
                {
                    StatusMessage = "Нет данных. Загрузите контрольные точки с сервера.";
                    System.Diagnostics.Debug.WriteLine("ControlPoints пуст после загрузки из БД");
                }
                else
                {
                    StatusMessage = $"Загружено {CountAllNodes(ControlPoints)} точек из БД";
                    System.Diagnostics.Debug.WriteLine($"Загружено дерево с {ControlPoints.Count} корневыми элементами");

                    // Для отладки: выводим структуру дерева
                    PrintTreeStructure(ControlPoints);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки точек из БД: {ex.Message}");
                StatusMessage = "Ошибка загрузки из БД";
            }
        }


        // Вспомогательный метод для подсчета всех узлов в дереве
        private int CountAllNodes(IEnumerable<ControlPoint> nodes)
        {
            if (nodes == null) return 0;

            int count = 0;
            foreach (var node in nodes)
            {
                count++;
                count += CountAllNodes(node.Children);
            }
            return count;
        }

        // Метод для отладки структуры дерева
        private void PrintTreeStructure(IEnumerable<ControlPoint> nodes, int level = 0)
        {
            foreach (var node in nodes)
            {
                var indent = new string(' ', level * 2);
                System.Diagnostics.Debug.WriteLine($"{indent}- {node.Name} (ID: {node.Id}, Parent: {node.ParentId})");
                PrintTreeStructure(node.Children, level + 1);
            }
        }

        private void BuildTree(ControlPoint parent, List<ControlPoint> allPoints)
        {
            if (parent == null || allPoints == null) return;

            parent.Children.Clear();
            var children = allPoints.Where(p => p.ParentId == parent.Id).ToList();
            foreach (var child in children)
            {
                BuildTree(child, allPoints);
                parent.Children.Add(child);
            }
        }

        private async Task TakeMeasurement()
        {
            if (SelectedControlPoint?.HasMeasurements != true)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    "Выберите точку с возможностью замера", "OK");
                return;
            }

            try
            {
                var measurementPage = new MeasurementPage(SelectedControlPoint, this);
                await Application.Current.MainPage.Navigation.PushAsync(measurementPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка перехода к замеру: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка открытия страницы замера: {ex.Message}", "OK");
            }
        }

        public void AddMeasurement(Measurement measurement)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    Measurements.Add(measurement);
                    await _measurementRepository.SaveAsync(measurement);
                    await LoadPendingMeasurements();

                    StatusMessage = $"Замер сохранен (всего: {Measurements.Count})";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения замера: {ex.Message}");
                }
            });
        }

        private async Task LoadPendingMeasurements()
        {
            try
            {
                var measurements = await _measurementRepository.GetAllAsync();
                PendingMeasurementsCount = measurements.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки ожидающих замеров: {ex.Message}");
            }
        }

        private async Task UploadMeasurements()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "Выгрузка замеров...";

            try
            {
                var measurements = await _measurementRepository.GetAllAsync();
                if (!measurements.Any())
                {
                    StatusMessage = "Нет данных для выгрузки";
                    await Application.Current.MainPage.DisplayAlert("Информация",
                        "Нет данных для выгрузки", "OK");
                    return;
                }

                var success = await _apiService.UploadMeasurementsAsync(measurements);
                if (success)
                {
                    await _measurementRepository.DeleteAllAsync();
                    Measurements.Clear();
                    await LoadPendingMeasurements();

                    StatusMessage = "Данные успешно выгружены";
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        $"Успешно выгружено {measurements.Count} замеров", "OK");
                }
                else
                {
                    StatusMessage = "Ошибка выгрузки";
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        "Ошибка выгрузки данных на сервер", "OK");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка выгрузки";
                System.Diagnostics.Debug.WriteLine($"Ошибка выгрузки замеров: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    $"Ошибка выгрузки: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearMeasurements()
        {
            if (PendingMeasurementsCount == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Информация",
                    "Нет данных для очистки", "OK");
                return;
            }

            var result = await Application.Current.MainPage.DisplayAlert("Подтверждение",
                $"Очистить все {PendingMeasurementsCount} локальных замеров?", "Да", "Нет");

            if (result)
            {
                try
                {
                    await _measurementRepository.DeleteAllAsync();
                    Measurements.Clear();
                    await LoadPendingMeasurements();

                    StatusMessage = "Замеры очищены";
                    await Application.Current.MainPage.DisplayAlert("Успех",
                        "Локальные замеры очищены", "OK");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка очистки замеров: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Ошибка",
                        $"Ошибка очистки: {ex.Message}", "OK");
                }
            }
        }

        private async Task RefreshData()
        {
            await LoadControlPointsFromDatabase();
            await LoadPendingMeasurements();
            StatusMessage = "Данные обновлены";
        }

        public async void OnAppearing()
        {
            await RefreshData();
        }

        public async void OnDisappearing()
        {
            try
            {
                // Отписываемся от событий
                BluetoothController.LeScanner.NewData -= OnVibrometerDataReceived;
                _nfcService.TagScanned -= OnTagScanned;
                _nfcService?.StopListening();

                // Останавливаем опрос виброметра
                if (IsPollingVibrometer)
                {
                    IsPollingVibrometer = false;
                    await Task.Delay(1000);
                }

                // Останавливаем BLE сканер
                BluetoothController.LeScanner.Stop();

                StatusMessage = "Ресурсы освобождены";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при очистке ресурсов: {ex.Message}");
            }
        }
    }
}