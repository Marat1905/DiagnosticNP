using DiagnosticNP.Models.Equipment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Api
{
    public interface IEquipmentApiService
    {
        Task<List<EquipmentNode>> GetEquipmentStructureAsync();
        Task<bool> UploadMeasurementsAsync(List<MeasurementData> measurements);
    }

    public class MockEquipmentApiService : IEquipmentApiService
    {
        public async Task<List<EquipmentNode>> GetEquipmentStructureAsync()
        {
            await Task.Delay(1000); // Имитация задержки сети

            var nodes = new List<EquipmentNode>();

            // Создаем полную структуру оборудования согласно предоставленному дереву
            CreateBdmStructure(nodes);
            CreateRpoStructure(nodes);
            CreateConstantTopStructure(nodes);
            CreateConstantBottomStructure(nodes);
            CreatePksStructure(nodes);

            return nodes;
        }

        private void CreateBdmStructure(List<EquipmentNode> nodes)
        {
            // БДМ - корневой узел
            var bdm = new EquipmentNode { Id = "BDM", ParentId = null, Name = "БДМ", NodeType = NodeType.Equipment, HasChildren = true };
            nodes.Add(bdm);

            // Мокрая часть
            var wetPart = new EquipmentNode { Id = "BDM_WET", ParentId = "BDM", Name = "Мокрая часть", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(wetPart);

            // Элементы мокрой части
            var topGrid = new EquipmentNode { Id = "BDM_WET_TOP_GRID", ParentId = "BDM_WET", Name = "Верхняя сетка", NodeType = NodeType.Component, NfcFilter = "Верхняя сетка", HasChildren = false };
            nodes.Add(topGrid);

            var gridRollerShaft = new EquipmentNode { Id = "BDM_WET_GRID_ROLLER", ParentId = "BDM_WET", Name = "Сеткоповоротный вал", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(gridRollerShaft);

            // Лицевой подшипник
            var frontBearing = new EquipmentNode { Id = "BDM_WET_FRONT_BEARING", ParentId = "BDM_WET", Name = "Лицевой подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Лицевой подшипник", HasChildren = true };
            nodes.Add(frontBearing);

            var frontBearingHorizontal = new EquipmentNode { Id = "BDM_WET_FRONT_BEARING_H", ParentId = "BDM_WET_FRONT_BEARING", Name = "Горизонтальная", NodeType = NodeType.Direction, HasChildren = false };
            nodes.Add(frontBearingHorizontal);

            var frontBearingVertical = new EquipmentNode { Id = "BDM_WET_FRONT_BEARING_V", ParentId = "BDM_WET_FRONT_BEARING", Name = "Вертикальная", NodeType = NodeType.Direction, HasChildren = false };
            nodes.Add(frontBearingVertical);

            var frontBearingAxial = new EquipmentNode { Id = "BDM_WET_FRONT_BEARING_A", ParentId = "BDM_WET_FRONT_BEARING", Name = "Осевая", NodeType = NodeType.Direction, HasChildren = false };
            nodes.Add(frontBearingAxial);

            var driveBearing1 = new EquipmentNode { Id = "BDM_WET_DRIVE_BEARING1", ParentId = "BDM_WET", Name = "Приводной подшипник", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(driveBearing1);

            var gridLeadingShaft = new EquipmentNode { Id = "BDM_WET_GRID_LEADING", ParentId = "BDM_WET", Name = "Сетковедущий вал", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(gridLeadingShaft);

            var driveBearing2 = new EquipmentNode { Id = "BDM_WET_DRIVE_BEARING2", ParentId = "BDM_WET", Name = "Приводной подшипник", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(driveBearing2);

            var frontBearing2 = new EquipmentNode { Id = "BDM_WET_FRONT_BEARING2", ParentId = "BDM_WET", Name = "Лицевой подшипник", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(frontBearing2);

            // Нижняя сетка
            var bottomGrid = new EquipmentNode { Id = "BDM_BOTTOM_GRID", ParentId = "BDM", Name = "Нижняя сетка", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(bottomGrid);

            var gauchShaft = new EquipmentNode { Id = "BDM_BOTTOM_GAUCH", ParentId = "BDM_BOTTOM_GRID", Name = "Гауч вал", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(gauchShaft);

            var bottomGridRoller = new EquipmentNode { Id = "BDM_BOTTOM_GRID_ROLLER", ParentId = "BDM_BOTTOM_GRID", Name = "Сеткоповоротный вал", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(bottomGridRoller);

            // Прессовая часть
            var pressPart = new EquipmentNode { Id = "BDM_PRESS", ParentId = "BDM", Name = "Прессовая часть", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(pressPart);

            // Пресс 1
            CreatePressStructure(nodes, "1", "BDM_PRESS");

            // Пресс 2
            CreatePressStructure(nodes, "2", "BDM_PRESS");

            // Пресс 3
            CreatePressStructure(nodes, "3", "BDM_PRESS");
        }

        private void CreatePressStructure(List<EquipmentNode> nodes, string pressNumber, string parentId)
        {
            var press = new EquipmentNode { Id = $"{parentId}_PRESS{pressNumber}", ParentId = parentId, Name = $"{pressNumber} Пресс", NodeType = NodeType.Component, HasChildren = true };
            nodes.Add(press);

            var shafts = new EquipmentNode { Id = $"{press.Id}_SHAFTS", ParentId = press.Id, Name = "Валы и валики", NodeType = NodeType.Component, HasChildren = false };
            nodes.Add(shafts);

            // Электродвигатель
            var motor = new EquipmentNode { Id = $"{press.Id}_MOTOR", ParentId = press.Id, Name = "Электродвигатель", NodeType = NodeType.Component, HasChildren = true };
            nodes.Add(motor);

            // Передний подшипник электродвигателя
            var motorFrontBearing = new EquipmentNode { Id = $"{motor.Id}_FRONT", ParentId = motor.Id, Name = "Передний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Передний подшипник", HasChildren = true };
            nodes.Add(motorFrontBearing);

            CreateDirectionNodes(nodes, motorFrontBearing.Id);

            // Задний подшипник электродвигателя
            var motorRearBearing = new EquipmentNode { Id = $"{motor.Id}_REAR", ParentId = motor.Id, Name = "Задний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Задний подшипник", HasChildren = true };
            nodes.Add(motorRearBearing);

            CreateDirectionNodes(nodes, motorRearBearing.Id);

            // Редуктор
            var reducer = new EquipmentNode { Id = $"{press.Id}_REDUCER", ParentId = press.Id, Name = "Редуктор", NodeType = NodeType.Component, HasChildren = true };
            nodes.Add(reducer);

            // Передний подшипник редуктора
            var reducerFrontBearing = new EquipmentNode { Id = $"{reducer.Id}_FRONT", ParentId = reducer.Id, Name = "Передний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Передний подшипник", HasChildren = true };
            nodes.Add(reducerFrontBearing);

            CreateDirectionNodes(nodes, reducerFrontBearing.Id);

            // Задний подшипник редуктора
            var reducerRearBearing = new EquipmentNode { Id = $"{reducer.Id}_REAR", ParentId = reducer.Id, Name = "Задний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Задний подшипник", HasChildren = true };
            nodes.Add(reducerRearBearing);

            CreateDirectionNodes(nodes, reducerRearBearing.Id);
        }

        private void CreateDirectionNodes(List<EquipmentNode> nodes, string parentId)
        {
            var horizontal = new EquipmentNode { Id = $"{parentId}_H", ParentId = parentId, Name = "Горизонтальная", NodeType = NodeType.Direction, HasChildren = false };
            nodes.Add(horizontal);

            var vertical = new EquipmentNode { Id = $"{parentId}_V", ParentId = parentId, Name = "Вертикальная", NodeType = NodeType.Direction, HasChildren = false };
            nodes.Add(vertical);

            var axial = new EquipmentNode { Id = $"{parentId}_A", ParentId = parentId, Name = "Осевая", NodeType = NodeType.Direction, HasChildren = false };
            nodes.Add(axial);
        }

        private void CreateRpoStructure(List<EquipmentNode> nodes)
        {
            var rpo = new EquipmentNode { Id = "RPO", ParentId = null, Name = "РПО", NodeType = NodeType.Equipment, HasChildren = true };
            nodes.Add(rpo);

            var pumpEquipment = new EquipmentNode { Id = "RPO_PUMP_EQUIPMENT", ParentId = "RPO", Name = "Насосное оборудование", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(pumpEquipment);

            CreatePumpStructure(nodes, "1", "RPO_PUMP_EQUIPMENT");
        }

        private void CreateConstantTopStructure(List<EquipmentNode> nodes)
        {
            var constantTop = new EquipmentNode { Id = "CONSTANT_TOP", ParentId = null, Name = "Постоянная часть верх", NodeType = NodeType.Equipment, HasChildren = true };
            nodes.Add(constantTop);

            var pumpEquipment = new EquipmentNode { Id = "CONSTANT_TOP_PUMP", ParentId = "CONSTANT_TOP", Name = "Насосное оборудование", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(pumpEquipment);

            CreatePumpStructure(nodes, "1", "CONSTANT_TOP_PUMP");
        }

        private void CreateConstantBottomStructure(List<EquipmentNode> nodes)
        {
            var constantBottom = new EquipmentNode { Id = "CONSTANT_BOTTOM", ParentId = null, Name = "Постоянная часть низ", NodeType = NodeType.Equipment, HasChildren = true };
            nodes.Add(constantBottom);

            var pumpEquipment = new EquipmentNode { Id = "CONSTANT_BOTTOM_PUMP", ParentId = "CONSTANT_BOTTOM", Name = "Насосное оборудование", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(pumpEquipment);

            CreatePumpStructure(nodes, "1", "CONSTANT_BOTTOM_PUMP");
        }

        private void CreatePksStructure(List<EquipmentNode> nodes)
        {
            var pks = new EquipmentNode { Id = "PKS", ParentId = null, Name = "ПКС", NodeType = NodeType.Equipment, HasChildren = true };
            nodes.Add(pks);

            var pumpEquipment = new EquipmentNode { Id = "PKS_PUMP_EQUIPMENT", ParentId = "PKS", Name = "Насосное оборудование", NodeType = NodeType.Section, HasChildren = true };
            nodes.Add(pumpEquipment);

            CreatePumpStructure(nodes, "1", "PKS_PUMP_EQUIPMENT");
        }

        private void CreatePumpStructure(List<EquipmentNode> nodes, string pumpNumber, string parentId)
        {
            var pump = new EquipmentNode { Id = $"{parentId}_PUMP{pumpNumber}", ParentId = parentId, Name = $"Насос {pumpNumber}", NodeType = NodeType.Component, HasChildren = true };
            nodes.Add(pump);

            // Электродвигатель насоса
            var pumpMotor = new EquipmentNode { Id = $"{pump.Id}_MOTOR", ParentId = pump.Id, Name = "Электродвигатель", NodeType = NodeType.Component, HasChildren = true };
            nodes.Add(pumpMotor);

            // Передний подшипник электродвигателя насоса
            var pumpMotorFront = new EquipmentNode { Id = $"{pumpMotor.Id}_FRONT", ParentId = pumpMotor.Id, Name = "Передний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Передний подшипник", HasChildren = true };
            nodes.Add(pumpMotorFront);

            CreateDirectionNodes(nodes, pumpMotorFront.Id);

            // Задний подшипник электродвигателя насоса
            var pumpMotorRear = new EquipmentNode { Id = $"{pumpMotor.Id}_REAR", ParentId = pumpMotor.Id, Name = "Задний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Задний подшипник", HasChildren = true };
            nodes.Add(pumpMotorRear);

            CreateDirectionNodes(nodes, pumpMotorRear.Id);

            // Насос
            var pumpUnit = new EquipmentNode { Id = $"{pump.Id}_UNIT", ParentId = pump.Id, Name = "Насос", NodeType = NodeType.Component, HasChildren = true };
            nodes.Add(pumpUnit);

            // Передний подшипник насоса
            var pumpUnitFront = new EquipmentNode { Id = $"{pumpUnit.Id}_FRONT", ParentId = pumpUnit.Id, Name = "Передний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Передний подшипник", HasChildren = true };
            nodes.Add(pumpUnitFront);

            CreateDirectionNodes(nodes, pumpUnitFront.Id);

            // Задний подшипник насоса
            var pumpUnitRear = new EquipmentNode { Id = $"{pumpUnit.Id}_REAR", ParentId = pumpUnit.Id, Name = "Задний подшипник", NodeType = NodeType.MeasurementPoint, NfcFilter = "Задний подшипник", HasChildren = true };
            nodes.Add(pumpUnitRear);

            CreateDirectionNodes(nodes, pumpUnitRear.Id);
        }
    

        public async Task<bool> UploadMeasurementsAsync(List<MeasurementData> measurements)
        {
            await Task.Delay(2000); // Имитация загрузки
            return true; // Имитация успешной загрузки
        }
    }
}