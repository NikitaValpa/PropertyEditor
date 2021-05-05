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
        private Dictionary<string, object> propsToClient = new Dictionary<string, object>();
        private Dictionary<string, Func<JsonElement,object>> Converter = new Dictionary<string, Func<JsonElement,object>>
        {
            ["SByte"] = jsonElem => { return jsonElem.GetSByte(); },
            ["Byte"] = jsonElem => { return jsonElem.GetByte(); },
            ["Int16"] = jsonElem => { return jsonElem.GetInt16(); },
            ["UInt16"] = jsonElem => { return jsonElem.GetUInt16(); },
            ["Int32"] = jsonElem => { return jsonElem.GetInt32(); },
            ["UInt32"] = jsonElem => { return jsonElem.GetUInt32(); },
            ["Int64"] = jsonElem => { return jsonElem.GetInt64(); },
            ["UInt64"] = jsonElem => { return jsonElem.GetUInt64(); },
        };
        private static object _obj;
        private readonly List<string> ValidTypes = new List<string>() {
            "SByte",
            "Byte",
            "Int16",
            "UInt16",
            "Int32",
            "UInt32",
            "Int64",
            "UInt64",
            "String"
        };
        public PropertyEditorHub(ILogger<PropertyEditorHub> logger)
        {
            _logger = logger;
        }

        private PropertyInfo[] Parser(Type type) //метод для парсинга свойств объектов
        {
            var props = new List<PropertyInfo>();
            foreach (var prop in type.GetProperties()) {
                if (prop.CanRead && prop.CanWrite)
                {//если у свойства есть и getter и setter идём дальше
                    if (ValidTypes.Contains(prop.PropertyType.Name))
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
         * Конец)
         */
        private object NumberConverter(Dictionary<string, object> JSprops) {
            var NETprops = Parser(_obj?.GetType());
            foreach (var JSname in JSprops.Keys) {
                foreach (var NETName in NETprops) {
                    if (NETName.Name == JSname) {
                        return Converter[NETName.PropertyType.Name]((JsonElement)JSprops[JSname]);
                    }
                }
            }
            return 0;//безопасим наше присваивание, ведь 0 поддерживают все целочисленные типы данных
        }

        public async Task Edit(object obj) {//метод в котором происходит чтение свойст объекта и отправка их клиенту для изменения
            try
            {
                _obj = obj;
                var ParsedProperties = Parser(_obj?.GetType());
                foreach (var prop in ParsedProperties) {
                    propsToClient.Add(prop.Name, prop.GetValue(_obj));
                }

                await Clients?.All.SendAsync("Recieve", propsToClient);//так как это веб приложение, то я не придумал ничего лучше, чем просто принудительно отправлять json объект нашему клиенту на js при подключении к хабу
            }
            catch (Exception ex) {
                _logger.LogError("При попытке парсинга свойств и отправке их клиенту произошла ошибка: " + ex.Message + " stackTrace \n" + ex.StackTrace);//для разработчика
                Clients?.All.SendAsync("Error", "Ошибка!", "При чтении свойств объекта на сервере, произошла ошибка, для того чтобы попробовать снова, обновите страничку");//для клиента
            }
            
        }

        public async Task Recieve(Dictionary<string, object> props) {//метод который принимает уже новый объект от клиента и меняет состояние того объекта который лежит и ждёт у нас в куче
            try
            {
                foreach (var prop in Parser(_obj?.GetType()))//это для отладки, чтобы видеть, что в куче есть наш объект со свойствами доступными для изменения и в них есть какое-то значение
                {
                    _logger.LogInformation($"Значение свойства {prop.Name} = " + prop.GetValue(_obj) + " объекта " + _obj?.GetType().Name + " до изменения");//это уже по сути для отладки, чтобы увидеть, что изменения произошли
                }


                foreach (var item in props.Keys)//собственно говоря меняем состояние объекта!
                { 
                    var value = (JsonElement)props[item];//апкастим до jsonElement, потому что именно такой тип всегда приходит к нам от клиента
                    if (value.ValueKind == JsonValueKind.Number)
                    {
                        _obj?.GetType().GetProperty(item).SetValue(_obj, NumberConverter(props));
                    }
                    else {
                        _obj?.GetType().GetProperty(item).SetValue(_obj, value.GetString());
                    }
                    
                }

                foreach (var prop in Parser(_obj?.GetType())) {//это уже по сути для отладки, чтобы увидеть, что изменения произошли
                    _logger.LogInformation($"Значение свойства {prop.Name} = " + prop.GetValue(_obj) + " объекта " + _obj?.GetType().Name + " после изменения");
                }

                props.Add("Edited", true);//если нигде не возникло исключения, значит можно сигнплизировать клиенту, что объект успешно изменён
                await Clients?.All.SendAsync("Recieve", props);//шлём обратно клиенту изменённый объект
            }
            catch (Exception ex)
            {
                _logger.LogError("При попытке изменения и отправки обратно клиенту объекта произошла ошибка: " + ex.Message + " stackTrace \n" + ex.StackTrace);//для разработчика
                Clients?.All.SendAsync("Error", "Ошибка!", "При изменении свойств объекта на сервере, произошла ошибка, для того чтобы попробовать снова, обновите страничку");//для клиента
            }
        }
        public override async Task OnConnectedAsync()
        {
            await Edit(new MyType1());
            await base.OnConnectedAsync();
        }

    }
    class MyType1 {//собственно прототип нашего объекта, здесь может быть любой самописный тип
        public int MyIntProperty { get; set; } = 5;
        public string MyStringProperty { get; set; } = "Hello world";
    }
}
