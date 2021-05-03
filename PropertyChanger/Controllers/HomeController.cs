using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PropertyChanger.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace PropertyChanger.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BinaryFormatter formater = new BinaryFormatter();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        [HttpPost]
        public IActionResult Desiarilizer(IFormFile NETObject) {
            if (NETObject == null) {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Message = "Выберите файл!" });
            }
            if (NETObject.ContentType == "text/plain") {   
                    try
                    {
                        var obj = formater.Deserialize(NETObject.OpenReadStream());//пытаемся десиарилизовать поток из файла
                    }
                    catch (Exception ex) {
                        _logger.LogError("При попытке десиарилизации произошла ошибка " + ex.Message);
                        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Message = ex.Message + " \n " + ex.StackTrace });
                    }
            }
            return Ok();
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
