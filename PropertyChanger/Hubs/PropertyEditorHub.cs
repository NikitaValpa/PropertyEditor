using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyChanger.Hubs
{
    public class PropertyEditorHub : Hub
    {
        private readonly ILogger<PropertyEditorHub> _logger;
        private readonly Dictionary<string, Func<JsonElement,object>> Converter = new Dictionary<string, Func<JsonElement,object>>
        {//можно очень удобно конфигурировать список поддерживаемых типов просто комментируя ненужные
            ["SByte"] = jsonElem => { return jsonElem.GetSByte(); },
            ["Byte"] = jsonElem => { return jsonElem.GetByte(); },
            ["Int16"] = jsonElem => { return jsonElem.GetInt16(); },
            ["UInt16"] = jsonElem => { return jsonElem.GetUInt16(); },
            ["Int32"] = jsonElem => { return jsonElem.GetInt32(); },
            ["UInt32"] = jsonElem => { return jsonElem.GetUInt32(); },
            ["Int64"] = jsonElem => { return jsonElem.GetInt64(); },
            ["UInt64"] = jsonElem => { return jsonElem.GetUInt64(); },
            ["String"] = jsonElem => { return jsonElem.GetString(); },
        };
        private static object _obj;
        public PropertyEditorHub(ILogger<PropertyEditorHub> logger)
        {
            _logger = logger;
        }

        private PropertyInfo[] PropertyFilter(Type type) //метод для парсинга свойств объектов
        {
            var props = new List<PropertyInfo>();
            foreach (var prop in type.GetProperties()) {
                if (prop.CanRead && prop.CanWrite)
                {//если у свойства есть и getter и setter идём дальше
                    if (Converter.Keys.Contains(prop.PropertyType.Name))
                    {//если тип свойства соответсвует любому из допустимых идём дальше
                        if (prop.GetIndexParameters().Length == 0)
                        {//если свойство не индексатор, это то что нам нужно :)
                            props.Add(prop);
                        }
                    }
                }
            }
            return props.ToArray();
        }
        /*Так, тут потребуется много комментариев, поехали.
         * Суть в следующем, от клиента нам приходит объект с ключом и значением, ключ, это имя нашего свойства, а значение, это структура JsonElement 
         * просто приведенная к object. Так вот, нам надо взять этот JsonElement и привести его к одному из целочисленных типов данных, которое поддерживает
         * наше приложение, для этого есть словарь конвертер. В него запиханы функции, которые как раз и приводят наш jsonElement например к int32 или int16 и т.д.
         * А доступ к функциям конвертера мы получаем по ключу, который является названием типа. Сам тип к которому нам нужно привести jsonElement мы получаем
         * путем сопоставления имени свойства которое к нам пришло от клиента и имени свойства в нашем .NET объекте.
         * Если всё проходит гладко, то после получения уже приведённого значения, мы присваиваем его свойству
         * Конец)
         */
        private bool PropertySetter(Dictionary<string, object> JSprops) {
            var NETprops = PropertyFilter(_obj?.GetType());
            foreach (var JSPropName in JSprops.Keys) {
                foreach (var NETprop in NETprops) {
                    if (NETprop.Name == JSPropName) {
                        var convertedValue = Converter[NETprop.PropertyType.Name]((JsonElement)JSprops[JSPropName]);
                        NETprop.SetValue(_obj, convertedValue);
                    }
                }
            }
            /*По сути можно было бы сделать метод void, но почему бы нам не возвращать true если все прошло успешно*/
            return true;//Если не на каком из этапов не выкинуто исключение, значит всё прошло гладко
        }

        public async Task InitializeObject(object obj) {//метод в котором происходит чтение свойст объекта и отправка их клиенту для изменения
            try
            {
                if (_obj == null) {
                    _obj = obj;
                }
                
                var FilteredProperties = PropertyFilter(_obj?.GetType());
                Dictionary<string, object> propsToClient = new Dictionary<string, object>();
                foreach (var prop in FilteredProperties) {
                    //здесь мы просто к поддерживаемым целочисленным типам добавляем их MaxValue и MinValue для валидирования на клиенте 
                    if (Converter.Keys.Take(8).FirstOrDefault(str => str == prop.PropertyType.Name) != null)
                    {
                        var max = prop.PropertyType.GetField("MaxValue").GetValue(_obj);
                        var min = prop.PropertyType.GetField("MinValue").GetValue(_obj);
                        propsToClient.Add(prop.Name, new { value = prop.GetValue(_obj), valueType = prop.PropertyType.Name, max = max, min = min });
                    }
                    else {
                        propsToClient.Add(prop.Name, new { value = prop.GetValue(_obj), valueType = prop.PropertyType.Name });
                    }
                }

                await Clients?.All.SendAsync("Recieve", propsToClient);//так как это веб приложение, то я не придумал ничего лучше, чем просто принудительно отправлять json объект нашему клиенту на js при подключении к хабу
            }
            catch (Exception ex) {
                _logger.LogError("При попытке парсинга свойств и отправке их клиенту произошла ошибка: " + ex.Message + "\n stackTrace: \n" + ex.StackTrace);//для разработчика
                Clients?.All.SendAsync("Error", "Ошибка!", "При чтении свойств объекта на сервере, произошла ошибка, для того чтобы попробовать снова, обновите страничку");//для клиента
            }
            
        }

        public async Task Edit(Dictionary<string, object> props) {//метод который принимает уже новый объект от клиента и меняет состояние того объекта который лежит и ждёт у нас в куче
            try
            {
                if (props.ContainsKey("Edited")) {
                    props.Remove("Edited");
                }

                foreach (var prop in PropertyFilter(_obj?.GetType()))//это для отладки, чтобы видеть, что в куче есть наш объект со свойствами доступными для изменения и в них есть какое-то значение
                {
                    _logger.LogInformation($"Значение свойства {prop.Name} = " + prop.GetValue(_obj) + " объекта " + _obj?.GetType().Name + " до изменения");//это уже по сути для отладки, чтобы увидеть, что изменения произошли
                }
                /*Процесс десиарилизации свойств которые приходят с клиента*/
                Dictionary<string, object> DesiarilazeDictionary = new Dictionary<string, object>();
                foreach (var prop in props) {
                    var jsDes = (JsonElement)prop.Value;
                    DesiarilazeDictionary.Add(prop.Key, jsDes.GetProperty("value"));
                }
                PropertySetter(DesiarilazeDictionary);//собственно говоря меняем состояние объекта!


                foreach (var prop in PropertyFilter(_obj?.GetType()))
                {//это уже по сути для отладки, чтобы увидеть, что изменения произошли
                    _logger.LogInformation($"Значение свойства {prop.Name} = " + prop.GetValue(_obj) + " объекта " + _obj?.GetType().Name + " после изменения");
                }
                
                props.Add("Edited", new { value = true });//если нигде не возникло исключения, значит можно сигнплизировать клиенту, что объект успешно изменён
                

                await Clients?.All.SendAsync("Recieve", props);//шлём обратно клиенту свойства для отрисовки
            }
            catch (Exception ex)
            {
                _logger.LogError("При попытке изменения и отправки обратно клиенту объекта произошла ошибка: " + ex.Message + "\n stackTrace: \n" + ex.StackTrace);//для разработчика
                Clients?.All.SendAsync("Error", "Ошибка!", "При изменении свойств объекта на сервере, произошла ошибка, для того чтобы попробовать снова, обновите страничку");//для клиента
            }
        }
        public override async Task OnConnectedAsync()
        {
            await InitializeObject(new MyType1());//место входа для нашего кастомного объекта, экспериментируйте!!!
            await base.OnConnectedAsync();
        }

    }
    class MyType1 {//собственно прототип нашего объекта, здесь может быть любой самописный тип
        public int MyIntProperty { get; set; } = -5;
        public byte MyByteProp { get; set; } = 4;
        public sbyte MySByteProp { get; set; } = -65;
        public uint MyUIntProperty { get; set; } = 5;
        public string MyStringProperty { get; set; } = "Hello world";
    }
}
