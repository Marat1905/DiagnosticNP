using DiagnosticNP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiagnosticNP.Services.Api
{
    public class MockApiService : IApiService
    {
        public async Task<List<EquipmentNode>> GetEquipmentStructureAsync()
        {
            await Task.Delay(1000); // Имитация задержки сети

            var nodes = new List<EquipmentNode>();
            var idCounter = 1;

            // Функция для создания узла с автоинкрементом ID
            string CreateNode(string parentId, string name, string fullPath = null)
            {
                var node = new EquipmentNode
                {
                    Id = (idCounter++).ToString(),
                    ParentId = parentId,
                    Name = name,
                    FullPath = fullPath ?? name
                };
                nodes.Add(node);
                return node.Id;
            }

            // 1. БДМ
            var bdmId = CreateNode(null, "БДМ");

            // 1.1 Мокрая часть
            var wetPartId = CreateNode(bdmId, "Мокрая часть", "БДМ\\Мокрая часть");

            var upperGridId = CreateNode(wetPartId, "Верхняя сетка", "БДМ\\Мокрая часть\\Верхняя сетка");
            var netShaftId = CreateNode(wetPartId, "Сеткоповоротный вал", "БДМ\\Мокрая часть\\Сеткоповоротный вал");

            // Лицевой подшипник
            var frontBearingId = CreateNode(wetPartId, "Лицевой подшипник", "БДМ\\Мокрая часть\\Лицевой подшипник");
            CreateNode(frontBearingId, "Горизонтальная", "БДМ\\Мокрая часть\\Лицевой подшипник\\Горизонтальная");
            CreateNode(frontBearingId, "Вертикальная", "БДМ\\Мокрая часть\\Лицевой подшипник\\Вертикальная");
            CreateNode(frontBearingId, "Осевая", "БДМ\\Мокрая часть\\Лицевой подшипник\\Осевая");

            var driveBearingId = CreateNode(wetPartId, "Приводной подшипник", "БДМ\\Мокрая часть\\Приводной подшипник");
            var netDriveShaftId = CreateNode(wetPartId, "Сетковедущий вал", "БДМ\\Мокрая часть\\Сетковедущий вал");
            var frontBearing2Id = CreateNode(wetPartId, "Лицевой подшипник", "БДМ\\Мокрая часть\\Лицевой подшипник");

            // 1.2 Нижняя сетка
            var lowerGridId = CreateNode(bdmId, "Нижняя сетка", "БДМ\\Нижняя сетка");
            var gauchShaftId = CreateNode(lowerGridId, "Гауч вал", "БДМ\\Нижняя сетка\\Гауч вал");
            var netTurnShaftId = CreateNode(lowerGridId, "Сеткоповоротный вал", "БДМ\\Нижняя сетка\\Сеткоповоротный вал");

            // 1.3 Прессовая часть
            var pressPartId = CreateNode(bdmId, "Прессовая часть", "БДМ\\Прессовая часть");

            // 1.3.1 1 Пресс
            var press1Id = CreateNode(pressPartId, "1 Пресс", "БДМ\\Прессовая часть\\1 Пресс");
            CreateNode(press1Id, "Валы и валики", "БДМ\\Прессовая часть\\1 Пресс\\Валы и валики");

            // Электродвигатель 1 Пресс
            var motor1Id = CreateNode(press1Id, "Электродвигатель", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель");

            var frontBearingMotor1Id = CreateNode(motor1Id, "Передний подшипник", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotor1Id, "Вертикальная", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotor1Id, "Горизонтальная", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotor1Id, "Осевая", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotor1Id = CreateNode(motor1Id, "Задний подшипник", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotor1Id, "Вертикальная", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotor1Id, "Горизонтальная", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotor1Id, "Осевая", "БДМ\\Прессовая часть\\1 Пресс\\Электродвигатель\\Задний подшипник\\Осевая");

            // Редуктор 1 Пресс
            var reducer1Id = CreateNode(press1Id, "Редуктор", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор");

            var frontBearingReducer1Id = CreateNode(reducer1Id, "Передний подшипник", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Передний подшипник");
            CreateNode(frontBearingReducer1Id, "Вертикальная", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingReducer1Id, "Горизонтальная", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingReducer1Id, "Осевая", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Передний подшипник\\Осевая");

            var rearBearingReducer1Id = CreateNode(reducer1Id, "Задний подшипник", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Задний подшипник");
            CreateNode(rearBearingReducer1Id, "Вертикальная", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingReducer1Id, "Горизонтальная", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingReducer1Id, "Осевая", "БДМ\\Прессовая часть\\1 Пресс\\Редуктор\\Задний подшипник\\Осевая");

            // 1.3.2 2 Пресс
            var press2Id = CreateNode(pressPartId, "2 Пресс", "БДМ\\Прессовая часть\\2 Пресс");
            CreateNode(press2Id, "Валы и валики", "БДМ\\Прессовая часть\\2 Пресс\\Валы и валики");

            // Электродвигатель 2 Пресс
            var motor2Id = CreateNode(press2Id, "Электродвигатель", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель");

            var frontBearingMotor2Id = CreateNode(motor2Id, "Передний подшипник", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotor2Id, "Вертикальная", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotor2Id, "Горизонтальная", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotor2Id, "Осевая", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotor2Id = CreateNode(motor2Id, "Задний подшипник", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotor2Id, "Вертикальная", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotor2Id, "Горизонтальная", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotor2Id, "Осевая", "БДМ\\Прессовая часть\\2 Пресс\\Электродвигатель\\Задний подшипник\\Осевая");

            // Редуктор 2 Пресс
            var reducer2Id = CreateNode(press2Id, "Редуктор", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор");

            var frontBearingReducer2Id = CreateNode(reducer2Id, "Передний подшипник", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Передний подшипник");
            CreateNode(frontBearingReducer2Id, "Вертикальная", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingReducer2Id, "Горизонтальная", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingReducer2Id, "Осевая", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Передний подшипник\\Осевая");

            var rearBearingReducer2Id = CreateNode(reducer2Id, "Задний подшипник", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Задний подшипник");
            CreateNode(rearBearingReducer2Id, "Вертикальная", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingReducer2Id, "Горизонтальная", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingReducer2Id, "Осевая", "БДМ\\Прессовая часть\\2 Пресс\\Редуктор\\Задний подшипник\\Осевая");

            // 1.3.3 3 Пресс
            var press3Id = CreateNode(pressPartId, "3 Пресс", "БДМ\\Прессовая часть\\3 Пресс");
            CreateNode(press3Id, "Валы и валики", "БДМ\\Прессовая часть\\3 Пресс\\Валы и валики");

            // Электродвигатель 3 Пресс
            var motor3Id = CreateNode(press3Id, "Электродвигатель", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель");

            var frontBearingMotor3Id = CreateNode(motor3Id, "Передний подшипник", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotor3Id, "Вертикальная", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotor3Id, "Горизонтальная", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotor3Id, "Осевая", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotor3Id = CreateNode(motor3Id, "Задний подшипник", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotor3Id, "Вертикальная", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotor3Id, "Горизонтальная", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotor3Id, "Осевая", "БДМ\\Прессовая часть\\3 Пресс\\Электродвигатель\\Задний подшипник\\Осевая");

            // Редуктор 3 Пресс
            var reducer3Id = CreateNode(press3Id, "Редуктор", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор");

            var frontBearingReducer3Id = CreateNode(reducer3Id, "Передний подшипник", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Передний подшипник");
            CreateNode(frontBearingReducer3Id, "Вертикальная", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingReducer3Id, "Горизонтальная", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingReducer3Id, "Осевая", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Передний подшипник\\Осевая");

            var rearBearingReducer3Id = CreateNode(reducer3Id, "Задний подшипник", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Задний подшипник");
            CreateNode(rearBearingReducer3Id, "Вертикальная", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingReducer3Id, "Горизонтальная", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingReducer3Id, "Осевая", "БДМ\\Прессовая часть\\3 Пресс\\Редуктор\\Задний подшипник\\Осевая");

            // 2. РПО
            var rpoId = CreateNode(null, "РПО");

            // 2.1 Насосное оборудование
            var pumpEquipmentRpoId = CreateNode(rpoId, "Насосное оборудование", "РПО\\Насосное оборудование");

            // 2.1.1 Насос 1
            var pump1RpoId = CreateNode(pumpEquipmentRpoId, "Насос 1", "РПО\\Насосное оборудование\\Насос 1");

            // Электродвигатель Насос 1 РПО
            var motorPump1RpoId = CreateNode(pump1RpoId, "Электродвигатель", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель");

            var frontBearingMotorPump1RpoId = CreateNode(motorPump1RpoId, "Передний подшипник", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotorPump1RpoId, "Вертикальная", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotorPump1RpoId, "Горизонтальная", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotorPump1RpoId, "Осевая", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotorPump1RpoId = CreateNode(motorPump1RpoId, "Задний подшипник", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotorPump1RpoId, "Вертикальная", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotorPump1RpoId, "Горизонтальная", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotorPump1RpoId, "Осевая", "РПО\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Осевая");

            // Насос Насос 1 РПО
            var pumpPump1RpoId = CreateNode(pump1RpoId, "Насос", "РПО\\Насосное оборудование\\Насос 1\\Насос");

            var frontBearingPumpPump1RpoId = CreateNode(pumpPump1RpoId, "Передний подшипник", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник");
            CreateNode(frontBearingPumpPump1RpoId, "Вертикальная", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingPumpPump1RpoId, "Горизонтальная", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingPumpPump1RpoId, "Осевая", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Осевая");

            var rearBearingPumpPump1RpoId = CreateNode(pumpPump1RpoId, "Задний подшипник", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник");
            CreateNode(rearBearingPumpPump1RpoId, "Вертикальная", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingPumpPump1RpoId, "Горизонтальная", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingPumpPump1RpoId, "Осевая", "РПО\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Осевая");

            // 3. Постоянная часть верх
            var constPartTopId = CreateNode(null, "Постоянная часть верх");

            // 3.1 Насосное оборудование
            var pumpEquipmentConstTopId = CreateNode(constPartTopId, "Насосное оборудование", "Постоянная часть верх\\Насосное оборудование");

            // 3.1.1 Насос 1
            var pump1ConstTopId = CreateNode(pumpEquipmentConstTopId, "Насос 1", "Постоянная часть верх\\Насосное оборудование\\Насос 1");

            // Электродвигатель Насос 1 Постоянная часть верх
            var motorPump1ConstTopId = CreateNode(pump1ConstTopId, "Электродвигатель", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель");

            var frontBearingMotorPump1ConstTopId = CreateNode(motorPump1ConstTopId, "Передний подшипник", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotorPump1ConstTopId, "Вертикальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotorPump1ConstTopId, "Горизонтальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotorPump1ConstTopId, "Осевая", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotorPump1ConstTopId = CreateNode(motorPump1ConstTopId, "Задний подшипник", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotorPump1ConstTopId, "Вертикальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotorPump1ConstTopId, "Горизонтальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotorPump1ConstTopId, "Осевая", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Осевая");

            // Насос Насос 1 Постоянная часть верх
            var pumpPump1ConstTopId = CreateNode(pump1ConstTopId, "Насос", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос");

            var frontBearingPumpPump1ConstTopId = CreateNode(pumpPump1ConstTopId, "Передний подшипник", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник");
            CreateNode(frontBearingPumpPump1ConstTopId, "Вертикальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingPumpPump1ConstTopId, "Горизонтальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingPumpPump1ConstTopId, "Осевая", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Осевая");

            var rearBearingPumpPump1ConstTopId = CreateNode(pumpPump1ConstTopId, "Задний подшипник", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник");
            CreateNode(rearBearingPumpPump1ConstTopId, "Вертикальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingPumpPump1ConstTopId, "Горизонтальная", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingPumpPump1ConstTopId, "Осевая", "Постоянная часть верх\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Осевая");

            // 4. Постоянная часть низ
            var constPartBottomId = CreateNode(null, "Постоянная часть низ");

            // 4.1 Насосное оборудование
            var pumpEquipmentConstBottomId = CreateNode(constPartBottomId, "Насосное оборудование", "Постоянная часть низ\\Насосное оборудование");

            // 4.1.1 Насос 1
            var pump1ConstBottomId = CreateNode(pumpEquipmentConstBottomId, "Насос 1", "Постоянная часть низ\\Насосное оборудование\\Насос 1");

            // Электродвигатель Насос 1 Постоянная часть низ
            var motorPump1ConstBottomId = CreateNode(pump1ConstBottomId, "Электродвигатель", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель");

            var frontBearingMotorPump1ConstBottomId = CreateNode(motorPump1ConstBottomId, "Передний подшипник", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotorPump1ConstBottomId, "Вертикальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotorPump1ConstBottomId, "Горизонтальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotorPump1ConstBottomId, "Осевая", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotorPump1ConstBottomId = CreateNode(motorPump1ConstBottomId, "Задний подшипник", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotorPump1ConstBottomId, "Вертикальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotorPump1ConstBottomId, "Горизонтальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotorPump1ConstBottomId, "Осевая", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Осевая");

            // Насос Насос 1 Постоянная часть низ
            var pumpPump1ConstBottomId = CreateNode(pump1ConstBottomId, "Насос", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос");

            var frontBearingPumpPump1ConstBottomId = CreateNode(pumpPump1ConstBottomId, "Передний подшипник", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник");
            CreateNode(frontBearingPumpPump1ConstBottomId, "Вертикальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingPumpPump1ConstBottomId, "Горизонтальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingPumpPump1ConstBottomId, "Осевая", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Осевая");

            var rearBearingPumpPump1ConstBottomId = CreateNode(pumpPump1ConstBottomId, "Задний подшипник", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник");
            CreateNode(rearBearingPumpPump1ConstBottomId, "Вертикальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingPumpPump1ConstBottomId, "Горизонтальная", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingPumpPump1ConstBottomId, "Осевая", "Постоянная часть низ\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Осевая");

            // 5. ПКС
            var pksId = CreateNode(null, "ПКС");

            // 5.1 Насосное оборудование
            var pumpEquipmentPksId = CreateNode(pksId, "Насосное оборудование", "ПКС\\Насосное оборудование");

            // 5.1.1 Насос 1
            var pump1PksId = CreateNode(pumpEquipmentPksId, "Насос 1", "ПКС\\Насосное оборудование\\Насос 1");

            // Электродвигатель Насос 1 ПКС
            var motorPump1PksId = CreateNode(pump1PksId, "Электродвигатель", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель");

            var frontBearingMotorPump1PksId = CreateNode(motorPump1PksId, "Передний подшипник", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник");
            CreateNode(frontBearingMotorPump1PksId, "Вертикальная", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingMotorPump1PksId, "Горизонтальная", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingMotorPump1PksId, "Осевая", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Передний подшипник\\Осевая");

            var rearBearingMotorPump1PksId = CreateNode(motorPump1PksId, "Задний подшипник", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник");
            CreateNode(rearBearingMotorPump1PksId, "Вертикальная", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingMotorPump1PksId, "Горизонтальная", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingMotorPump1PksId, "Осевая", "ПКС\\Насосное оборудование\\Насос 1\\Электродвигатель\\Задний подшипник\\Осевая");

            // Насос Насос 1 ПКС
            var pumpPump1PksId = CreateNode(pump1PksId, "Насос", "ПКС\\Насосное оборудование\\Насос 1\\Насос");

            var frontBearingPumpPump1PksId = CreateNode(pumpPump1PksId, "Передний подшипник", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник");
            CreateNode(frontBearingPumpPump1PksId, "Вертикальная", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Вертикальная");
            CreateNode(frontBearingPumpPump1PksId, "Горизонтальная", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Горизонтальная");
            CreateNode(frontBearingPumpPump1PksId, "Осевая", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Передний подшипник\\Осевая");

            var rearBearingPumpPump1PksId = CreateNode(pumpPump1PksId, "Задний подшипник", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник");
            CreateNode(rearBearingPumpPump1PksId, "Вертикальная", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Вертикальная");
            CreateNode(rearBearingPumpPump1PksId, "Горизонтальная", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Горизонтальная");
            CreateNode(rearBearingPumpPump1PksId, "Осевая", "ПКС\\Насосное оборудование\\Насос 1\\Насос\\Задний подшипник\\Осевая");

            return nodes;
        }

        public async Task<bool> UploadMeasurementsAsync(List<Measurement> measurements)
        {
            await Task.Delay(2000); // Имитация задержки сети

            try
            {
                // Логируем выгрузку для отладки
                System.Diagnostics.Debug.WriteLine($"Выгружаем {measurements.Count} замеров:");

                foreach (var measurement in measurements)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {measurement.EquipmentNodeId}: " +
                        $"{measurement.MeasurementType} - " +
                        $"V: {measurement.Velocity}, T: {measurement.Temperature}, " +
                        $"A: {measurement.Acceleration}, K: {measurement.Kurtosis}, " +
                        $"Time: {measurement.MeasurementTime}");
                }

                // В реальном приложении здесь будет вызов REST API
                // Пока возвращаем успех для 90% случаев для тестирования
                var random = new Random();
                return random.Next(0, 10) > 0; // 90% успеха
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка выгрузки замеров: {ex.Message}");
                return false;
            }
        }
    }
}