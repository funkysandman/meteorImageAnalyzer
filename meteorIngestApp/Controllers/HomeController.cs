using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using meteorIngestApp.Models;
using System.Net.Http;

namespace meteorIngestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        public IActionResult images()
        {
           
                IEnumerable<skyImageWS.SkyImage> images = null;

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
                    //HTTP GET
                    var responseTask = client.GetAsync("skyImages");
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                    var readTask = result.Content.ReadAsAsync<IList<skyImageWS.SkyImage>>();
                    readTask.Wait();

                    images = readTask.Result;
                }
                    else //web api sent error response 
                    {
                        //log response status here..

                        images = Enumerable.Empty<skyImageWS.SkyImage>();

                        ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                    }
                }
                return View(images);
            }

        public IActionResult details(int id)
        {

            skyImageWS.SkyImage image = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
                //HTTP GET
                var responseTask = client.GetAsync(String.Format("skyImages/full/{0}",id));
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<skyImageWS.SkyImage>();
                    readTask.Wait();

                    image = readTask.Result;
                }
                else //web api sent error response 
                {
                    //log response status here..

                    image = new skyImageWS.SkyImage();//should be empty

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
            return View(image);
        }


        public IActionResult delete(int id)
        {

            skyImageWS.SkyImage image = null;

            using (var client = new HttpClient())
            {
               // client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
                client.BaseAddress = new Uri("https://localhost:44322/api/");
                //HTTP GET
                var responseTask = client.DeleteAsync(String.Format("skyImages/{0}", id));
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<skyImageWS.SkyImage>();
                    readTask.Wait();

                    image = readTask.Result;
                }
                else //web api sent error response 
                {
                    //log response status here..

                    image = new skyImageWS.SkyImage();//should be empty

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
            return View(image);
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
