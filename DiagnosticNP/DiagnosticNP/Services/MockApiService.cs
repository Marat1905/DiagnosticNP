using DiagnosticNP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiagnosticNP.Services
{
    public class MockApiService : IApiService
    {
        public async Task<List<ControlPoint>> GetControlPointsAsync()
        {
            await Task.Delay(1000); // Имитация задержки сети

            System.Diagnostics.Debug.WriteLine("=== СОЗДАНИЕ ПОЛНОЙ СТРУКТУРЫ ОБОРУДОВАНИЯ ===");

            var controlPoints = new List<ControlPoint>();
            int currentId = 1;

            // Создаем корневой узел "Оборудование"
            var equipment = CreateControlPoint(ref currentId, null, "Оборудование", "Оборудование", true);

            // БДМ
            var bdm = CreateControlPoint(ref currentId, equipment.Id, "БДМ", "Оборудование\\БДМ", true);
            equipment.AddChild(bdm);

            // Мокрая часть БДМ
            var wetPart = CreateControlPoint(ref currentId, bdm.Id, "Мокрая часть", "Оборудование\\БДМ\\Мокрая часть", true);
            bdm.AddChild(wetPart);

            // Элементы мокрой части
            var upperGrid = CreateControlPoint(ref currentId, wetPart.Id, "Верхняя сетка", "Оборудование\\БДМ\\Мокрая часть\\Верхняя сетка");
            var gridTurnShaft = CreateControlPoint(ref currentId, wetPart.Id, "Сеткоповоротный вал", "Оборудование\\БДМ\\Мокрая часть\\Сеткоповоротный вал");

            // Лицевой подшипник (мокрая часть)
            var frontBearingWet = CreateControlPoint(ref currentId, wetPart.Id, "Лицевой подшипник", "Оборудование\\БДМ\\Мокрая часть\\Лицевой подшипник", true);
            var frontBearingHorizontal = CreateMeasurementPoint(ref currentId, frontBearingWet.Id, "Горизонтальная", "Horizontal");
            var frontBearingVertical = CreateMeasurementPoint(ref currentId, frontBearingWet.Id, "Вертикальная", "Vertical");
            var frontBearingAxial = CreateMeasurementPoint(ref currentId, frontBearingWet.Id, "Осевая", "Axial");

            frontBearingWet.AddChild(frontBearingHorizontal);
            frontBearingWet.AddChild(frontBearingVertical);
            frontBearingWet.AddChild(frontBearingAxial);

            var driveBearing1 = CreateControlPoint(ref currentId, wetPart.Id, "Приводной подшипник", "Оборудование\\БДМ\\Мокрая часть\\Приводной подшипник");
            var gridLeadingShaft = CreateControlPoint(ref currentId, wetPart.Id, "Сетковедущий вал", "Оборудование\\БДМ\\Мокрая часть\\Сетковедущий вал");
            var driveBearing2 = CreateControlPoint(ref currentId, wetPart.Id, "Приводной подшипник", "Оборудование\\БДМ\\Мокрая часть\\Приводной подшипник");
            var frontBearingWet2 = CreateControlPoint(ref currentId, wetPart.Id, "Лицевой подшипник", "Оборудование\\БДМ\\Мокрая часть\\Лицевой подшипник");

            // Добавляем все элементы мокрой части
            wetPart.AddChild(upperGrid);
            wetPart.AddChild(gridTurnShaft);
            wetPart.AddChild(frontBearingWet);
            wetPart.AddChild(driveBearing1);
            wetPart.AddChild(gridLeadingShaft);
            wetPart.AddChild(driveBearing2);
            wetPart.AddChild(frontBearingWet2);

            // Нижняя сетка БДМ
            var lowerGrid = CreateControlPoint(ref currentId, bdm.Id, "Нижняя сетка", "Оборудование\\БДМ\\Нижняя сетка", true);
            bdm.AddChild(lowerGrid);

            var gaugeShaft = CreateControlPoint(ref currentId, lowerGrid.Id, "Гауч вал", "Оборудование\\БДМ\\Нижняя сетка\\Гауч вал");
            var gridTurnShaftLower = CreateControlPoint(ref currentId, lowerGrid.Id, "Сеткоповоротный вал", "Оборудование\\БДМ\\Нижняя сетка\\Сеткоповоротный вал");

            lowerGrid.AddChild(gaugeShaft);
            lowerGrid.AddChild(gridTurnShaftLower);

            // Прессовая часть БДМ
            var pressPart = CreateControlPoint(ref currentId, bdm.Id, "Прессовая часть", "Оборудование\\БДМ\\Прессовая часть", true);
            bdm.AddChild(pressPart);

            // Пресс 1
            var press1 = CreateControlPoint(ref currentId, pressPart.Id, "1 Пресс", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс", true);
            pressPart.AddChild(press1);

            var shaftsAndRolls1 = CreateControlPoint(ref currentId, press1.Id, "Валы и валики", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Валы и валики");

            // Электродвигатель Пресс 1
            var motor1 = CreateControlPoint(ref currentId, press1.Id, "Электродвигатель", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель", true);
            press1.AddChild(motor1);

            var frontBearingMotor1 = CreateControlPoint(ref currentId, motor1.Id, "Передний подшипник", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Передний подшипник", true);
            var frontBearingMotor1Vertical = CreateMeasurementPoint(ref currentId, frontBearingMotor1.Id, "Вертикальная", "Vertical");
            var frontBearingMotor1Horizontal = CreateMeasurementPoint(ref currentId, frontBearingMotor1.Id, "Горизонтальная", "Horizontal");
            var frontBearingMotor1Axial = CreateMeasurementPoint(ref currentId, frontBearingMotor1.Id, "Осевая", "Axial");

            frontBearingMotor1.AddChild(frontBearingMotor1Vertical);
            frontBearingMotor1.AddChild(frontBearingMotor1Horizontal);
            frontBearingMotor1.AddChild(frontBearingMotor1Axial);

            var rearBearingMotor1 = CreateControlPoint(ref currentId, motor1.Id, "Задний подшипник", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Задний подшипник", true);
            var rearBearingMotor1Vertical = CreateMeasurementPoint(ref currentId, rearBearingMotor1.Id, "Вертикальная", "Vertical");
            var rearBearingMotor1Horizontal = CreateMeasurementPoint(ref currentId, rearBearingMotor1.Id, "Горизонтальная", "Horizontal");
            var rearBearingMotor1Axial = CreateMeasurementPoint(ref currentId, rearBearingMotor1.Id, "Осевая", "Axial");

            rearBearingMotor1.AddChild(rearBearingMotor1Vertical);
            rearBearingMotor1.AddChild(rearBearingMotor1Horizontal);
            rearBearingMotor1.AddChild(rearBearingMotor1Axial);

            motor1.AddChild(frontBearingMotor1);
            motor1.AddChild(rearBearingMotor1);

            // Редуктор Пресс 1
            var reducer1 = CreateControlPoint(ref currentId, press1.Id, "Редуктор", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Редуктор", true);
            press1.AddChild(reducer1);

            var frontBearingReducer1 = CreateControlPoint(ref currentId, reducer1.Id, "Передний подшипник", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Передний подшипник", true);
            var frontBearingReducer1Vertical = CreateMeasurementPoint(ref currentId, frontBearingReducer1.Id, "Вертикальная", "Vertical");
            var frontBearingReducer1Horizontal = CreateMeasurementPoint(ref currentId, frontBearingReducer1.Id, "Горизонтальная", "Horizontal");
            var frontBearingReducer1Axial = CreateMeasurementPoint(ref currentId, frontBearingReducer1.Id, "Осевая", "Axial");

            frontBearingReducer1.AddChild(frontBearingReducer1Vertical);
            frontBearingReducer1.AddChild(frontBearingReducer1Horizontal);
            frontBearingReducer1.AddChild(frontBearingReducer1Axial);

            var rearBearingReducer1 = CreateControlPoint(ref currentId, reducer1.Id, "Задний подшипник", "Оборудование\\БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Задний подшипник", true);
            var rearBearingReducer1Vertical = CreateMeasurementPoint(ref currentId, rearBearingReducer1.Id, "Вертикальная", "Vertical");
            var rearBearingReducer1Horizontal = CreateMeasurementPoint(ref currentId, rearBearingReducer1.Id, "Горизонтальная", "Horizontal");
            var rearBearingReducer1Axial = CreateMeasurementPoint(ref currentId, rearBearingReducer1.Id, "Осевая", "Axial");

            rearBearingReducer1.AddChild(rearBearingReducer1Vertical);
            rearBearingReducer1.AddChild(rearBearingReducer1Horizontal);
            rearBearingReducer1.AddChild(rearBearingReducer1Axial);

            reducer1.AddChild(frontBearingReducer1);
            reducer1.AddChild(rearBearingReducer1);

            press1.AddChild(shaftsAndRolls1);
            press1.AddChild(motor1);
            press1.AddChild(reducer1);

            // Пресс 2 (аналогично Пресс 1)
            var press2 = CreateControlPoint(ref currentId, pressPart.Id, "2 Пресс", "Оборудование\\БДМ\\Прессовая часть\\2 Пресс", true);
            pressPart.AddChild(press2);

            var shaftsAndRolls2 = CreateControlPoint(ref currentId, press2.Id, "Валы и валики", "Оборудование\\БДМ\\Прессовая часть\\2 Пресс\\Валы и валики");

            var motor2 = CreateControlPoint(ref currentId, press2.Id, "Электродвигатель", "Оборудование\\БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель", true);
            AddMotorStructure(ref currentId, motor2, "Оборудование\\БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель");

            var reducer2 = CreateControlPoint(ref currentId, press2.Id, "Редуктор", "Оборудование\\БДМ\\Прессовая часть\\2 Пресс\\Редуктор", true);
            AddReducerStructure(ref currentId, reducer2, "Оборудование\\БДМ\\Прессовая часть\\2 Пресс\\Редуктор");

            press2.AddChild(shaftsAndRolls2);
            press2.AddChild(motor2);
            press2.AddChild(reducer2);

            // Пресс 3 (аналогично Пресс 1)
            var press3 = CreateControlPoint(ref currentId, pressPart.Id, "3 Пресс", "Оборудование\\БДМ\\Прессовая часть\\3 Пресс", true);
            pressPart.AddChild(press3);

            var shaftsAndRolls3 = CreateControlPoint(ref currentId, press3.Id, "Валы и валики", "Оборудование\\БДМ\\Прессовая часть\\3 Пресс\\Валы и валики");

            var motor3 = CreateControlPoint(ref currentId, press3.Id, "Электродвигатель", "Оборудование\\БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель", true);
            AddMotorStructure(ref currentId, motor3, "Оборудование\\БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель");

            var reducer3 = CreateControlPoint(ref currentId, press3.Id, "Редуктор", "Оборудование\\БДМ\\Прессовая часть\\3 Пресс\\Редуктор", true);
            AddReducerStructure(ref currentId, reducer3, "Оборудование\\БДМ\\Прессовая часть\\3 Пресс\\Редуктор");

            press3.AddChild(shaftsAndRolls3);
            press3.AddChild(motor3);
            press3.AddChild(reducer3);

            // РПО
            var rpo = CreateControlPoint(ref currentId, equipment.Id, "РПО", "Оборудование\\РПО", true);
            equipment.AddChild(rpo);

            var pumpEquipmentRPO = CreateControlPoint(ref currentId, rpo.Id, "Насосное оборудование", "Оборудование\\РПО\\Насосное оборудование", true);
            rpo.AddChild(pumpEquipmentRPO);

            var pump1RPO = CreateControlPoint(ref currentId, pumpEquipmentRPO.Id, "Насос 1", "Оборудование\\РПО\\Насосное оборудование\\Насос 1", true);
            AddPumpStructure(ref currentId, pump1RPO, "Оборудование\\РПО\\Насосное оборудование\\Насос 1");
            pumpEquipmentRPO.AddChild(pump1RPO);

            // Постоянная часть верх
            var constantTop = CreateControlPoint(ref currentId, equipment.Id, "Постоянная часть верх", "Оборудование\\Постоянная часть верх", true);
            equipment.AddChild(constantTop);

            var pumpEquipmentTop = CreateControlPoint(ref currentId, constantTop.Id, "Насосное оборудование", "Оборудование\\Постоянная часть верх\\Насосное оборудование", true);
            constantTop.AddChild(pumpEquipmentTop);

            var pump1Top = CreateControlPoint(ref currentId, pumpEquipmentTop.Id, "Насос 1", "Оборудование\\Постоянная часть верх\\Насосное оборудование\\Насос 1", true);
            AddPumpStructure(ref currentId, pump1Top, "Оборудование\\Постоянная часть верх\\Насосное оборудование\\Насос 1");
            pumpEquipmentTop.AddChild(pump1Top);

            // Постоянная часть низ
            var constantBottom = CreateControlPoint(ref currentId, equipment.Id, "Постоянная часть низ", "Оборудование\\Постоянная часть низ", true);
            equipment.AddChild(constantBottom);

            var pumpEquipmentBottom = CreateControlPoint(ref currentId, constantBottom.Id, "Насосное оборудование", "Оборудование\\Постоянная часть низ\\Насосное оборудование", true);
            constantBottom.AddChild(pumpEquipmentBottom);

            var pump1Bottom = CreateControlPoint(ref currentId, pumpEquipmentBottom.Id, "Насос 1", "Оборудование\\Постоянная часть низ\\Насосное оборудование\\Насос 1", true);
            AddPumpStructure(ref currentId, pump1Bottom, "Оборудование\\Постоянная часть низ\\Насосное оборудование\\Насос 1");
            pumpEquipmentBottom.AddChild(pump1Bottom);

            // ПКС
            var pks = CreateControlPoint(ref currentId, equipment.Id, "ПКС", "Оборудование\\ПКС", true);
            equipment.AddChild(pks);

            var pumpEquipmentPKS = CreateControlPoint(ref currentId, pks.Id, "Насосное оборудование", "Оборудование\\ПКС\\Насосное оборудование", true);
            pks.AddChild(pumpEquipmentPKS);

            var pump1PKS = CreateControlPoint(ref currentId, pumpEquipmentPKS.Id, "Насос 1", "Оборудование\\ПКС\\Насосное оборудование\\Насос 1", true);
            AddPumpStructure(ref currentId, pump1PKS, "Оборудование\\ПКС\\Насосное оборудование\\Насос 1");
            pumpEquipmentPKS.AddChild(pump1PKS);

            controlPoints.Add(equipment);

            System.Diagnostics.Debug.WriteLine($"Создано полное дерево оборудования с {CountAllNodes(controlPoints)} узлами");
            PrintTreeStructure(controlPoints);

            return controlPoints;
        }

        public async Task<bool> UploadMeasurementsAsync(List<Measurement> measurements)
        {
            await Task.Delay(2000); // Имитация загрузки

            System.Diagnostics.Debug.WriteLine($"Выгружено {measurements?.Count ?? 0} замеров на сервер");

            // Всегда успешно для мока
            return true;
        }

        #region Вспомогательные методы

        private ControlPoint CreateControlPoint(ref int currentId, int? parentId, string name, string fullPath, bool isExpanded = false)
        {
            return new ControlPoint
            {
                Id = currentId++,
                ParentId = parentId,
                Name = name,
                FullPath = fullPath,
                IsExpanded = isExpanded
            };
        }

        private ControlPoint CreateMeasurementPoint(ref int currentId, int parentId, string name, string measurementType)
        {
            return new ControlPoint
            {
                Id = currentId++,
                ParentId = parentId,
                Name = name,
                FullPath = $"TODO_FULL_PATH\\{name}", // Полный путь будет построен при сохранении
                MeasurementType = measurementType,
                HasMeasurements = true
            };
        }

        private void AddMotorStructure(ref int currentId, ControlPoint motor, string basePath)
        {
            var frontBearing = CreateControlPoint(ref currentId, motor.Id, "Передний подшипник", $"{basePath}\\Передний подшипник", true);
            frontBearing.AddChild(CreateMeasurementPoint(ref currentId, frontBearing.Id, "Вертикальная", "Vertical"));
            frontBearing.AddChild(CreateMeasurementPoint(ref currentId, frontBearing.Id, "Горизонтальная", "Horizontal"));
            frontBearing.AddChild(CreateMeasurementPoint(ref currentId, frontBearing.Id, "Осевая", "Axial"));

            var rearBearing = CreateControlPoint(ref currentId, motor.Id, "Задний подшипник", $"{basePath}\\Задний подшипник", true);
            rearBearing.AddChild(CreateMeasurementPoint(ref currentId, rearBearing.Id, "Вертикальная", "Vertical"));
            rearBearing.AddChild(CreateMeasurementPoint(ref currentId, rearBearing.Id, "Горизонтальная", "Horizontal"));
            rearBearing.AddChild(CreateMeasurementPoint(ref currentId, rearBearing.Id, "Осевая", "Axial"));

            motor.AddChild(frontBearing);
            motor.AddChild(rearBearing);
        }

        private void AddReducerStructure(ref int currentId, ControlPoint reducer, string basePath)
        {
            var frontBearing = CreateControlPoint(ref currentId, reducer.Id, "Передний подшипник", $"{basePath}\\Передний подшипник", true);
            frontBearing.AddChild(CreateMeasurementPoint(ref currentId, frontBearing.Id, "Вертикальная", "Vertical"));
            frontBearing.AddChild(CreateMeasurementPoint(ref currentId, frontBearing.Id, "Горизонтальная", "Horizontal"));
            frontBearing.AddChild(CreateMeasurementPoint(ref currentId, frontBearing.Id, "Осевая", "Axial"));

            var rearBearing = CreateControlPoint(ref currentId, reducer.Id, "Задний подшипник", $"{basePath}\\Задний подшипник", true);
            rearBearing.AddChild(CreateMeasurementPoint(ref currentId, rearBearing.Id, "Вертикальная", "Vertical"));
            rearBearing.AddChild(CreateMeasurementPoint(ref currentId, rearBearing.Id, "Горизонтальная", "Horizontal"));
            rearBearing.AddChild(CreateMeasurementPoint(ref currentId, rearBearing.Id, "Осевая", "Axial"));

            reducer.AddChild(frontBearing);
            reducer.AddChild(rearBearing);
        }

        private void AddPumpStructure(ref int currentId, ControlPoint pump, string basePath)
        {
            var motor = CreateControlPoint(ref currentId, pump.Id, "Электродвигатель", $"{basePath}\\Электродвигатель", true);
            AddMotorStructure(ref currentId, motor, $"{basePath}\\Электродвигатель");

            var pumpUnit = CreateControlPoint(ref currentId, pump.Id, "Насос", $"{basePath}\\Насос", true);

            var frontBearingPump = CreateControlPoint(ref currentId, pumpUnit.Id, "Передний подшипник", $"{basePath}\\Насос\\Передний подшипник", true);
            frontBearingPump.AddChild(CreateMeasurementPoint(ref currentId, frontBearingPump.Id, "Вертикальная", "Vertical"));
            frontBearingPump.AddChild(CreateMeasurementPoint(ref currentId, frontBearingPump.Id, "Горизонтальная", "Horizontal"));
            frontBearingPump.AddChild(CreateMeasurementPoint(ref currentId, frontBearingPump.Id, "Осевая", "Axial"));

            var rearBearingPump = CreateControlPoint(ref currentId, pumpUnit.Id, "Задний подшипник", $"{basePath}\\Насос\\Задний подшипник", true);
            rearBearingPump.AddChild(CreateMeasurementPoint(ref currentId, rearBearingPump.Id, "Вертикальная", "Vertical"));
            rearBearingPump.AddChild(CreateMeasurementPoint(ref currentId, rearBearingPump.Id, "Горизонтальная", "Horizontal"));
            rearBearingPump.AddChild(CreateMeasurementPoint(ref currentId, rearBearingPump.Id, "Осевая", "Axial"));

            pumpUnit.AddChild(frontBearingPump);
            pumpUnit.AddChild(rearBearingPump);

            pump.AddChild(motor);
            pump.AddChild(pumpUnit);
        }

        private int CountAllNodes(List<ControlPoint> nodes)
        {
            int count = 0;
            foreach (var node in nodes)
            {
                count++;
                if (node.Children != null && node.Children.Any())
                    count += CountAllNodes(node.Children.ToList());
            }
            return count;
        }

        private void PrintTreeStructure(List<ControlPoint> nodes, int level = 0)
        {
            foreach (var node in nodes)
            {
                var indent = new string(' ', level * 2);
                var measurementInfo = node.HasMeasurements ? $" [ЗАМЕР: {node.MeasurementType}]" : "";
                System.Diagnostics.Debug.WriteLine($"{indent}- {node.Name} (ID: {node.Id}, ParentID: {node.ParentId}){measurementInfo}");

                if (node.Children != null && node.Children.Any())
                    PrintTreeStructure(node.Children.ToList(), level + 1);
            }
        }

        #endregion
    }
}