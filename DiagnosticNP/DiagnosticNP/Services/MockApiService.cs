using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiagnosticNP.Models;

namespace DiagnosticNP.Services
{
    public class MockApiService : IApiService
    {
        public async Task<List<EquipmentNode>> DownloadControlPointsAsync()
        {
            await Task.Delay(1000); // Имитация задержки сети

            var equipmentStructure = new List<EquipmentNode>();

            // Создаем корневые узлы
            var bdmNode = CreateEquipmentNode("БДМ", "БДМ", NodeType.Equipment);
            var rpoNode = CreateEquipmentNode("РПО", "РПО", NodeType.Equipment);
            var topNode = CreateEquipmentNode("Постоянная часть верх", "Постоянная часть верх", NodeType.Equipment);
            var bottomNode = CreateEquipmentNode("Постоянная часть низ", "Постоянная часть низ", NodeType.Equipment);
            var pksNode = CreateEquipmentNode("ПКС", "ПКС", NodeType.Equipment);

            // Заполняем БДМ
            bdmNode.Children = new List<EquipmentNode>
            {
                CreateWetPart(bdmNode.Id),
                CreateLowerGrid(bdmNode.Id),
                CreatePressPart(bdmNode.Id)
            };

            // Заполняем остальные узлы насосным оборудованием
            rpoNode.Children = CreatePumpEquipment(rpoNode.Id);
            topNode.Children = CreatePumpEquipment(topNode.Id);
            bottomNode.Children = CreatePumpEquipment(bottomNode.Id);
            pksNode.Children = CreatePumpEquipment(pksNode.Id);

            equipmentStructure.Add(bdmNode);
            equipmentStructure.Add(rpoNode);
            equipmentStructure.Add(topNode);
            equipmentStructure.Add(bottomNode);
            equipmentStructure.Add(pksNode);

            return equipmentStructure;
        }

        public async Task<bool> UploadMeasurementsAsync(List<MeasurementData> measurements)
        {
            await Task.Delay(1500); // Имитация загрузки
            System.Diagnostics.Debug.WriteLine($"Uploaded {measurements.Count} measurements");
            return true;
        }

        private EquipmentNode CreateEquipmentNode(string name, string path, NodeType type, string parentId = null)
        {
            return new EquipmentNode
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                FullPath = path,
                Type = type,
                ParentId = parentId,
                Children = new List<EquipmentNode>()
            };
        }

        private EquipmentNode CreateWetPart(string parentId)
        {
            var wetPart = CreateEquipmentNode("Мокрая часть", "БДМ/Мокрая часть", NodeType.Component, parentId);

            wetPart.Children = new List<EquipmentNode>
            {
                CreateEquipmentNode("Верхняя сетка", "БДМ/Мокрая часть/Верхняя сетка", NodeType.MeasurementPoint, wetPart.Id),
                CreateEquipmentNode("Сеткоповоротный вал", "БДМ/Мокрая часть/Сеткоповоротный вал", NodeType.MeasurementPoint, wetPart.Id),
                CreateBearingNode("Лицевой подшипник", "БДМ/Мокрая часть/Лицевой подшипник", wetPart.Id),
                CreateEquipmentNode("Приводной подшипник", "БДМ/Мокрая часть/Приводной подшипник", NodeType.MeasurementPoint, wetPart.Id),
                CreateEquipmentNode("Сетковедущий вал", "БДМ/Мокрая часть/Сетковедущий вал", NodeType.MeasurementPoint, wetPart.Id)
            };

            return wetPart;
        }

        private EquipmentNode CreateLowerGrid(string parentId)
        {
            var lowerGrid = CreateEquipmentNode("Нижняя сетка", "БДМ/Нижняя сетка", NodeType.Component, parentId);

            lowerGrid.Children = new List<EquipmentNode>
            {
                CreateEquipmentNode("Гауч вал", "БДМ/Нижняя сетка/Гауч вал", NodeType.MeasurementPoint, lowerGrid.Id),
                CreateEquipmentNode("Сеткоповоротный вал", "БДМ/Нижняя сетка/Сеткоповоротный вал", NodeType.MeasurementPoint, lowerGrid.Id)
            };

            return lowerGrid;
        }

        private EquipmentNode CreatePressPart(string parentId)
        {
            var pressPart = CreateEquipmentNode("Прессовая часть", "БДМ/Прессовая часть", NodeType.Component, parentId);

            pressPart.Children = new List<EquipmentNode>
            {
                CreatePressNode("1 Пресс", pressPart.Id),
                CreatePressNode("2 Пресс", pressPart.Id),
                CreatePressNode("3 Пресс", pressPart.Id)
            };

            return pressPart;
        }

        private EquipmentNode CreatePressNode(string pressName, string parentId)
        {
            var basePath = $"БДМ/Прессовая часть/{pressName}";
            var pressNode = CreateEquipmentNode(pressName, basePath, NodeType.Component, parentId);

            pressNode.Children = new List<EquipmentNode>
            {
                CreateEquipmentNode("Валы и валики", $"{basePath}/Валы и валики", NodeType.MeasurementPoint, pressNode.Id),
                CreateMotorNode($"{basePath}/Электродвигатель", pressNode.Id),
                CreateReducerNode($"{basePath}/Редуктор", pressNode.Id)
            };

            return pressNode;
        }

        private EquipmentNode CreateMotorNode(string path, string parentId)
        {
            var motorNode = CreateEquipmentNode("Электродвигатель", path, NodeType.MeasurementPoint, parentId);

            motorNode.Children = new List<EquipmentNode>
            {
                CreateBearingNode("Передний подшипник", $"{path}/Передний подшипник", motorNode.Id),
                CreateBearingNode("Задний подшипник", $"{path}/Задний подшипник", motorNode.Id)
            };

            return motorNode;
        }

        private EquipmentNode CreateReducerNode(string path, string parentId)
        {
            var reducerNode = CreateEquipmentNode("Редуктор", path, NodeType.MeasurementPoint, parentId);

            reducerNode.Children = new List<EquipmentNode>
            {
                CreateBearingNode("Передний подшипник", $"{path}/Передний подшипник", reducerNode.Id),
                CreateBearingNode("Задний подшипник", $"{path}/Задний подшипник", reducerNode.Id)
            };

            return reducerNode;
        }

        private EquipmentNode CreateBearingNode(string name, string path, string parentId)
        {
            var bearingNode = CreateEquipmentNode(name, path, NodeType.MeasurementPoint, parentId);

            bearingNode.Children = new List<EquipmentNode>
            {
                CreateDirectionNode("Вертикальная", $"{path}/Вертикальная", bearingNode.Id),
                CreateDirectionNode("Горизонтальная", $"{path}/Горизонтальная", bearingNode.Id),
                CreateDirectionNode("Осевая", $"{path}/Осевая", bearingNode.Id)
            };

            return bearingNode;
        }

        private EquipmentNode CreateDirectionNode(string name, string path, string parentId)
        {
            return CreateEquipmentNode(name, path, NodeType.Direction, parentId);
        }

        private List<EquipmentNode> CreatePumpEquipment(string parentId)
        {
            var pumpEquipment = CreateEquipmentNode("Насосное оборудование", "Насосное оборудование", NodeType.Component, parentId);

            var pump1 = CreateEquipmentNode("Насос 1", "Насосное оборудование/Насос 1", NodeType.MeasurementPoint, pumpEquipment.Id);

            pump1.Children = new List<EquipmentNode>
            {
                CreateMotorNode("Насосное оборудование/Насос 1/Электродвигатель", pump1.Id),
                CreatePumpNode("Насосное оборудование/Насос 1/Насос", pump1.Id)
            };

            pumpEquipment.Children = new List<EquipmentNode> { pump1 };

            return new List<EquipmentNode> { pumpEquipment };
        }

        private EquipmentNode CreatePumpNode(string path, string parentId)
        {
            var pumpNode = CreateEquipmentNode("Насос", path, NodeType.MeasurementPoint, parentId);

            pumpNode.Children = new List<EquipmentNode>
            {
                CreateBearingNode("Передний подшипник", $"{path}/Передний подшипник", pumpNode.Id),
                CreateBearingNode("Задний подшипник", $"{path}/Задний подшипник", pumpNode.Id)
            };

            return pumpNode;
        }
    }
}