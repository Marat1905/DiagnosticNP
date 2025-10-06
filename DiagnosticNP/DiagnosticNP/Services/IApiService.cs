using DiagnosticNP.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticNP.Services
{
    public interface IApiService
    {
        Task<List<ControlPoint>> GetControlPointsAsync();
        Task<bool> UploadMeasurementsAsync(List<MeasurementData> measurements);
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://your-api-url.com/api"; // Замените на реальный URL

        public ApiService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<List<ControlPoint>> GetControlPointsAsync()
        {
            try
            {
                // Для отладки используем мок-данные
                if (IsServerAvailable())
                {
                    var response = await _httpClient.GetAsync($"{BaseUrl}/controlpoints");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<ControlPoint>>(json);
                    }
                }

                // Возвращаем мок-данные если сервер недоступен
                return GetMockControlPoints();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return GetMockControlPoints();
            }
        }

        public async Task<bool> UploadMeasurementsAsync(List<MeasurementData> measurements)
        {
            try
            {
                if (IsServerAvailable())
                {
                    var json = JsonConvert.SerializeObject(measurements);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"{BaseUrl}/measurements", content);
                    return response.IsSuccessStatusCode;
                }

                // Для отладки всегда возвращаем true
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload Error: {ex.Message}");
                return false;
            }
        }

        private bool IsServerAvailable()
        {
            // Реализуйте проверку доступности сервера
            // Для отладки всегда возвращаем false чтобы использовать мок-данные
            return false;
        }

        private List<ControlPoint> GetMockControlPoints()
        {
            var points = new List<ControlPoint>();
            var id = 1;

            // 1 Сушильная группа
            var dryingGroup1 = new ControlPoint { Id = id++, Name = "1 Сушильная группа", Level = 0, FullPath = "1 Сушильная группа" };
            points.Add(dryingGroup1);

            // Верхняя сетка
            var topGrid = new ControlPoint { Id = id++, Name = "Верхняя сетка", Level = 1, ParentId = dryingGroup1.Id, ParentPath = dryingGroup1.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка" };
            points.Add(topGrid);

            // 1 Сушильный цилиндр (Верхняя сетка)
            var cylinder1Top = new ControlPoint { Id = id++, Name = "1 Сушильный цилиндр", Level = 2, ParentId = topGrid.Id, ParentPath = topGrid.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр" };
            points.Add(cylinder1Top);

            // Лицевой подшипник (1 Сушильный цилиндр - Верхняя сетка)
            var frontBearing1Top = new ControlPoint { Id = id++, Name = "Лицевой подшипник", Level = 3, ParentId = cylinder1Top.Id, ParentPath = cylinder1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник" };
            points.Add(frontBearing1Top);

            // Точки замера для лицевого подшипника (Верхняя сетка)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 4, ParentId = frontBearing1Top.Id, ParentPath = frontBearing1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 4, ParentId = frontBearing1Top.Id, ParentPath = frontBearing1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 4, ParentId = frontBearing1Top.Id, ParentPath = frontBearing1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Приводной подшипник (1 Сушильный цилиндр - Верхняя сетка)
            var driveBearing1Top = new ControlPoint { Id = id++, Name = "Приводной подшипник", Level = 3, ParentId = cylinder1Top.Id, ParentPath = cylinder1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник" };
            points.Add(driveBearing1Top);

            // Точки замера для приводного подшипника (Верхняя сетка)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 4, ParentId = driveBearing1Top.Id, ParentPath = driveBearing1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 4, ParentId = driveBearing1Top.Id, ParentPath = driveBearing1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 4, ParentId = driveBearing1Top.Id, ParentPath = driveBearing1Top.FullPath, FullPath = "1 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Нижняя сетка
            var bottomGrid = new ControlPoint { Id = id++, Name = "Нижняя сетка", Level = 1, ParentId = dryingGroup1.Id, ParentPath = dryingGroup1.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка" };
            points.Add(bottomGrid);

            // 2 Сушильный цилиндр (Нижняя сетка)
            var cylinder2Bottom = new ControlPoint { Id = id++, Name = "2 Сушильный цилиндр", Level = 2, ParentId = bottomGrid.Id, ParentPath = bottomGrid.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр" };
            points.Add(cylinder2Bottom);

            // Лицевой подшипник (2 Сушильный цилиндр - Нижняя сетка)
            var frontBearing2Bottom = new ControlPoint { Id = id++, Name = "Лицевой подшипник", Level = 3, ParentId = cylinder2Bottom.Id, ParentPath = cylinder2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Лицевой подшипник" };
            points.Add(frontBearing2Bottom);

            // Точки замера для лицевого подшипника (Нижняя сетка)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 4, ParentId = frontBearing2Bottom.Id, ParentPath = frontBearing2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Лицевой подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 4, ParentId = frontBearing2Bottom.Id, ParentPath = frontBearing2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Лицевой подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 4, ParentId = frontBearing2Bottom.Id, ParentPath = frontBearing2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Лицевой подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Приводной подшипник (2 Сушильный цилиндр - Нижняя сетка)
            var driveBearing2Bottom = new ControlPoint { Id = id++, Name = "Приводной подшипник", Level = 3, ParentId = cylinder2Bottom.Id, ParentPath = cylinder2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Приводной подшипник" };
            points.Add(driveBearing2Bottom);

            // Точки замера для приводного подшипника (Нижняя сетка)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 4, ParentId = driveBearing2Bottom.Id, ParentPath = driveBearing2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Приводной подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 4, ParentId = driveBearing2Bottom.Id, ParentPath = driveBearing2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Приводной подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 4, ParentId = driveBearing2Bottom.Id, ParentPath = driveBearing2Bottom.FullPath, FullPath = "1 Сушильная группа/Нижняя сетка/2 Сушильный цилиндр/Приводной подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Редуктор
            var reducer = new ControlPoint { Id = id++, Name = "Редуктор", Level = 1, ParentId = dryingGroup1.Id, ParentPath = dryingGroup1.FullPath, FullPath = "1 Сушильная группа/Редуктор" };
            points.Add(reducer);

            // Передний подшипник (Редуктор)
            var frontBearingReducer = new ControlPoint { Id = id++, Name = "Передний подшипник", Level = 2, ParentId = reducer.Id, ParentPath = reducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Передний подшипник" };
            points.Add(frontBearingReducer);

            // Точки замера для переднего подшипника (Редуктор)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 3, ParentId = frontBearingReducer.Id, ParentPath = frontBearingReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Передний подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 3, ParentId = frontBearingReducer.Id, ParentPath = frontBearingReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Передний подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 3, ParentId = frontBearingReducer.Id, ParentPath = frontBearingReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Передний подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Задний подшипник (Редуктор)
            var rearBearingReducer = new ControlPoint { Id = id++, Name = "Задний подшипник", Level = 2, ParentId = reducer.Id, ParentPath = reducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Задний подшипник" };
            points.Add(rearBearingReducer);

            // Точки замера для заднего подшипника (Редуктор)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 3, ParentId = rearBearingReducer.Id, ParentPath = rearBearingReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Задний подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 3, ParentId = rearBearingReducer.Id, ParentPath = rearBearingReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Задний подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 3, ParentId = rearBearingReducer.Id, ParentPath = rearBearingReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Задний подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Общая вибрация (Редуктор)
            var generalVibrationReducer = new ControlPoint { Id = id++, Name = "Общая вибрация", Level = 2, ParentId = reducer.Id, ParentPath = reducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Общая вибрация" };
            points.Add(generalVibrationReducer);

            // Точки замера для общей вибрации (Редуктор)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 3, ParentId = generalVibrationReducer.Id, ParentPath = generalVibrationReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Общая вибрация/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 3, ParentId = generalVibrationReducer.Id, ParentPath = generalVibrationReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Общая вибрация/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 3, ParentId = generalVibrationReducer.Id, ParentPath = generalVibrationReducer.FullPath, FullPath = "1 Сушильная группа/Редуктор/Общая вибрация/Осевая вибрация", IsMeasurementPoint = true });

            // Электродвигатель
            var electricMotor = new ControlPoint { Id = id++, Name = "Электродвигатель", Level = 1, ParentId = dryingGroup1.Id, ParentPath = dryingGroup1.FullPath, FullPath = "1 Сушильная группа/Электродвигатель" };
            points.Add(electricMotor);

            // Подшипники электродвигателя (можно добавить аналогично редуктору)
            var frontBearingMotor = new ControlPoint { Id = id++, Name = "Передний подшипник", Level = 2, ParentId = electricMotor.Id, ParentPath = electricMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Передний подшипник" };
            points.Add(frontBearingMotor);

            // Точки замера для переднего подшипника (Электродвигатель)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 3, ParentId = frontBearingMotor.Id, ParentPath = frontBearingMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Передний подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 3, ParentId = frontBearingMotor.Id, ParentPath = frontBearingMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Передний подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 3, ParentId = frontBearingMotor.Id, ParentPath = frontBearingMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Передний подшипник/Осевая вибрация", IsMeasurementPoint = true });

            var rearBearingMotor = new ControlPoint { Id = id++, Name = "Задний подшипник", Level = 2, ParentId = electricMotor.Id, ParentPath = electricMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Задний подшипник" };
            points.Add(rearBearingMotor);

            // Точки замера для заднего подшипника (Электродвигатель)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 3, ParentId = rearBearingMotor.Id, ParentPath = rearBearingMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Задний подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 3, ParentId = rearBearingMotor.Id, ParentPath = rearBearingMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Задний подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 3, ParentId = rearBearingMotor.Id, ParentPath = rearBearingMotor.FullPath, FullPath = "1 Сушильная группа/Электродвигатель/Задний подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // 2 Сушильная группа (аналогично первой, но с другим ID)
            var dryingGroup2 = new ControlPoint { Id = id++, Name = "2 Сушильная группа", Level = 0, FullPath = "2 Сушильная группа" };
            points.Add(dryingGroup2);

            // Верхняя сетка (2 группа)
            var topGrid2 = new ControlPoint { Id = id++, Name = "Верхняя сетка", Level = 1, ParentId = dryingGroup2.Id, ParentPath = dryingGroup2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка" };
            points.Add(topGrid2);

            // 1 Сушильный цилиндр (Верхняя сетка - 2 группа)
            var cylinder1Top2 = new ControlPoint { Id = id++, Name = "1 Сушильный цилиндр", Level = 2, ParentId = topGrid2.Id, ParentPath = topGrid2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр" };
            points.Add(cylinder1Top2);

            // Лицевой подшипник (1 Сушильный цилиндр - Верхняя сетка - 2 группа)
            var frontBearing1Top2 = new ControlPoint { Id = id++, Name = "Лицевой подшипник", Level = 3, ParentId = cylinder1Top2.Id, ParentPath = cylinder1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник" };
            points.Add(frontBearing1Top2);

            // Точки замера для лицевого подшипника (Верхняя сетка - 2 группа)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 4, ParentId = frontBearing1Top2.Id, ParentPath = frontBearing1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 4, ParentId = frontBearing1Top2.Id, ParentPath = frontBearing1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 4, ParentId = frontBearing1Top2.Id, ParentPath = frontBearing1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Лицевой подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Приводной подшипник (1 Сушильный цилиндр - Верхняя сетка - 2 группа)
            var driveBearing1Top2 = new ControlPoint { Id = id++, Name = "Приводной подшипник", Level = 3, ParentId = cylinder1Top2.Id, ParentPath = cylinder1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник" };
            points.Add(driveBearing1Top2);

            // Точки замера для приводного подшипника (Верхняя сетка - 2 группа)
            points.Add(new ControlPoint { Id = id++, Name = "Вертикальная вибрация", Level = 4, ParentId = driveBearing1Top2.Id, ParentPath = driveBearing1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник/Вертикальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Горизонтальная вибрация", Level = 4, ParentId = driveBearing1Top2.Id, ParentPath = driveBearing1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник/Горизонтальная вибрация", IsMeasurementPoint = true });
            points.Add(new ControlPoint { Id = id++, Name = "Осевая вибрация", Level = 4, ParentId = driveBearing1Top2.Id, ParentPath = driveBearing1Top2.FullPath, FullPath = "2 Сушильная группа/Верхняя сетка/1 Сушильный цилиндр/Приводной подшипник/Осевая вибрация", IsMeasurementPoint = true });

            // Добавьте остальные элементы для 2 Сушильной группы по аналогии с первой...

            return points;
        }
    }
}