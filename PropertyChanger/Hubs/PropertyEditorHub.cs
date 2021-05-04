using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyChanger.Hubs
{
    public class PropertyEditorHub:Hub
    {
        private readonly ILogger<PropertyEditorHub> _logger;
        private Dictionary<string, object> keyValuePairsToClient = new Dictionary<string, object>();
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

        private PropertyInfo[] Parser(Type type) //метод для парсинга объектов
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

        public async Task Edit(object obj) {
            try
            {
                var ParsedProperties = Parser(obj.GetType());
                foreach (var prop in ParsedProperties) {
                    keyValuePairsToClient.Add(prop.Name, prop.GetValue(obj));
                }
                
                await Clients?.All.SendAsync("Recieve", keyValuePairsToClient);//так как это веб приложение, то я не придумал ничего лучше, чем просто принудительно отправлять json объект нашему клиенту на js
            }
            catch (Exception ex) {
                _logger.LogError("При попытке парсинга свойств и отправке их клиенту произошла ошибка: " + ex.Message + " stackTrace \n" + ex.StackTrace);
            }
            
        }
        public override async Task OnConnectedAsync()
        {
            await Edit(new MyType1());
            await base.OnConnectedAsync();
        }

    }
    class MyType1 {
        public int MyIntProperty { get; set; } = 5;
        public string MyStringProperty { get; set; } = "Hello world";
    }
}
