using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using meteorIngestApp.Models;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.IO;
using PagedList;
using PagedList.Core;
using Microsoft.AspNetCore.Routing;

namespace meteorIngestApp.Controllers
{
    //




 public class PageModelList<T> : skyImageWS.SkyImage
{
    public IEnumerable<T> Items { get; set; }
    public int Count
    {
        get
        {
            if (Items == null)
            {
                return 0;
            }

            return Items.Count();
        }
    }

    public IPagedList<T> PList { get; set; }
}
//

public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> images(string sortOrder,string currentFilter,string searchString, int? pageNumber)
    //    public IActionResult images(string sortOrder, string currentFilter, string searchString, int? page)
        {
           
                IEnumerable<skyImageWS.SkyImage> images = null;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentPage"] = pageNumber;
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            using (var client = new HttpClient())
                {
                //client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
                client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
                //HTTP GET
                var responseTask = client.GetAsync("skyImages");
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                    var readTask = result.Content.ReadAsAsync<IList<skyImageWS.SkyImage>>();
                    readTask.Wait();
                    //
                    images = readTask.Result;

                    ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                    ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
                    var anImage = from a in images select a;
                    switch (sortOrder)
                    {
                        case "name_desc":
                            images = images.OrderByDescending(s => s.Filename);
                            break;
                        case "Date":
                            images = images.OrderBy(s => s.Date);
                            break;
                        case "date_desc":
                            images = images.OrderByDescending(s => s.Date);
                            break;
                        default:
                           
                            break;
                    }
                    //return View(await images.ToList());
                    //ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                    //ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

                    if (!String.IsNullOrEmpty(searchString))
                    {
                        images = images.Where(s => s.Filename.Contains(searchString)
                                               || s.Camera.Contains(searchString));
                    }
                    switch (sortOrder)
                    {
                        case "name_desc":
                            images = images.OrderByDescending(s => s.Filename);
                            break;
                        case "Date":
                            images = images.OrderBy(s => s.Date);
                            break;
                        case "date_desc":
                            images = images.OrderByDescending(s => s.Date);
                            break;
                        default:

                            break;
                    }



                    ////


                }
                    else //web api sent error response 
                    {
                        //log response status here..

                        images = Enumerable.Empty<skyImageWS.SkyImage>();

                        ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                    }
                }
            int pageSize = 25;

            ////return View(images.(pageNumber, pageSize));
            //return View( PaginatedList<skyImageWS.SkyImage>.CreateAsync(images, pageNumber ?? 1, pageSize));

           
            return View(await PaginatedList<skyImageWS.SkyImage>.CreateAsync(images, pageNumber ?? 1, pageSize));
       

    }

    

        public IActionResult edit(int id, string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {

            skyImageWS.SkyImage image = null;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentPage"] = pageNumber;
            ViewData["CurrentSearchString"] = searchString;
            TempData["CurrentPage"] = pageNumber;
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


        public IActionResult delete(int id, string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {

            skyImageWS.SkyImage image = null;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["CurrentPage"] = pageNumber;
            ViewData["CurrentSearchString"] = searchString;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
               // client.BaseAddress = new Uri("https://localhost:44322/api/");
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
            return RedirectToAction("Images", new RouteValueDictionary(
    new { controller = "Home", action = "Images", sortOrder= ViewData["CurrentSort"], currentFilter= ViewData["CurrentFilter"], searchString= ViewData["CurrentSearchString"], pageNumber = ViewData["CurrentPage"] }));
        }


        public IActionResult Save(skyImageWS.SkyImage si)
        {

            skyImageWS.SkyImage image = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://imageingest.azurewebsites.net/api/");
                //
                // var streamWriter = new StreamWriter(client..GetRequestStream());
                var putTask = client.PutAsJsonAsync<skyImageWS.SkyImage>(String.Format("skyImages/{0}",si.SkyImageId), si);
                putTask.Wait();

                var result = putTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    return RedirectToAction("Images", new RouteValueDictionary(
                        new { controller = "Home", action = "Images", sortOrder = ViewData["CurrentSort"], currentFilter = ViewData["CurrentFilter"], searchString = ViewData["CurrentSearchString"], pageNumber = TempData["CurrentPage"] }));
                }
            

              
               
                else //web api sent error response 
                {
                    //log response status here..

                    image = new skyImageWS.SkyImage();//should be empty

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                    return RedirectToAction("Images", new RouteValueDictionary(
    new { controller = "Home", action = "Images", sortOrder = ViewData["CurrentSort"], currentFilter = ViewData["CurrentFilter"], searchString = ViewData["CurrentSearchString"], pageNumber = ViewData["CurrentPage"] }));
                }
            }
           
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
