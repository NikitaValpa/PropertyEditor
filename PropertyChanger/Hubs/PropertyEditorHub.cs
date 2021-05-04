using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PropertyChanger.Hubs
{
    public class PropertyEditorHub:Hub
    {
        private readonly ILogger<PropertyEditorHub> _logger;
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
            //var ParsedProperties = Parser(obj.GetType());
            
            await Clients?.All.SendAsync("Recieve", obj);//так как это веб приложение, то я не придумал ничего лучше, чем просто принудительно отправлять json объект нашему клиенту на js
        }

    }
    class MyType1 {
        public int MyIntProperty { get; set; } = 5;
        public string MyStringProperty { get; set; } = "Hello world";
    }
}
