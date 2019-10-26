using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeteorIngestAPI.Models;
using System.IO;
using System.Drawing;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;

namespace meteorIngest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkyImagesController : ControllerBase
    {
        private readonly MeteorIngestContext _context;

        public SkyImagesController(MeteorIngestContext context)
        {
            _context = context;
        }

        // GET: api/SkyImages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SkyImage>>> GetSkyImages()
        {


            var imageList = _context.SkyImages
                    .FromSqlRaw("Select * from SkyImages a order by  (select count(*) from SkyImages c where c.imageSet = a.imageSet),(select max(score) from SkyObjectDetection where skyImageId=a.skyImageId and skyObjectClass=='meteor') desc")
                    .ToListAsync<SkyImage>();
            return await imageList;

            //var sortedImages = from si in _context.SkyImages
            //                   orderby ((from sod in si.detectedObjects where sod.skyObjectClass == "meteor" select sod).Max(c => c.score)) descending
            //                   select si;

            //return await sortedImages.ToListAsync();



            //return await _context.SkyImages.Include(c => c.detectedObjects)
            //    .ThenInclude(si => si.bbox)
            //    .ToListAsync();

            //     var highScores = from si in _context.SkyImages 
            //                      (from sod in si.detectedObjects where sod.skyObjectClass == "meteor"  
            //                             select sod.score).Max();

            //     //

            //     var highScores =
            //from skyImage in skyImages
            //group student by student.Year into studentGroup
            //select new
            //{
            //    Level = studentGroup.Key,
            //    HighestScore =
            //    (from student2 in studentGroup
            //     select student2.ExamScores.Average()).Max()
            //};

            //     //


            //     foreach (var item in highScores)
            //     {
            //         Console.WriteLine($"{item.Name,-15}{item.Score}");
            //     }



        }

        //// GET: api/SkyImages
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<SkyImage>>> GetSkyImagesOrderedByScore()
        //{

           



        //}

        // GET: api/SkyImages/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SkyImage>> GetSkyImage(int id)
        {
            //var skyImage = await _context.SkyImages.FindAsync(id);
            var skyImage = await _context.SkyImages
                .Include(c => c.detectedObjects)
                .ThenInclude(si => si.bbox)
                .FirstOrDefaultAsync(i => i.skyImageId == id);

                
                
            if (skyImage == null)
            {
                return NotFound();
            }
           
            return skyImage;
        
        }

        // GET: api/SkyImages/5
        [HttpGet("full/{id}")]
        public async Task<ActionResult<SkyImage>> GetSkyImageWithJPG(int id)
        {
            //var skyImage = await _context.SkyImages.FindAsync(id);
            var skyImage = await _context.SkyImages
                .Include(x => x.imageData)
                .Include(c => c.detectedObjects)
                .ThenInclude(si => si.bbox)
                .FirstOrDefaultAsync(i => i.skyImageId == id);



            if (skyImage == null)
            {
                return NotFound();
            }
            //get image from cloud storage

            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();



            CloudBlobContainer cloudBlobContainer = blobClient.GetContainerReference("found");
            CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename);



            MemoryStream memStream = new MemoryStream();

            await blockBlob.DownloadToStreamAsync(memStream);
            var byteArray = memStream.ToArray();
            string base64String = Convert.ToBase64String(byteArray);
            skyImage.imageData.imageData = base64String;
            return skyImage;

        }

        // PUT: api/SkyImages/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSkyImage(int id, [FromBody] SkyImage skyImage)
        {
            if (id != skyImage.skyImageId)
            {
                return BadRequest();
            }

            _context.Entry(skyImage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SkyImageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/SkyImages
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.


        [HttpPost]
        public async Task<ActionResult<SkyImage>> PostSkyImage([FromBody]SkyImage skyImage)
        {

          
            if (_context.SkyImages.Contains(skyImage))
            {
                return BadRequest();
            }
            //
            //check for image intersections
            //
            //new set or existing set
            var setID = findImageSet(skyImage);
            skyImage.imageSet = setID;

            //get file image
            var imageDataByteArray = Convert.FromBase64String(skyImage.imageData.imageData);

            //When creating a stream, you need to reset the position, without it you will see that you always write files with a 0 byte length. 
            var imageDataStream = new MemoryStream(imageDataByteArray);
            imageDataStream.Position = 0;

            //

            //save file to azure storage account
            //string storageConnection = CloudConfigurationManager.GetSetting("BlobStorageConnectionString");

            //create a block blob 
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            //create a container 
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");

            //create a container if it is not already exists

            if (await cloudBlobContainer.CreateIfNotExistsAsync())
            {

                await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            }

           

            //get Blob reference



            //
            //using (FileStream file = new FileStream(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename), FileMode.Create, System.IO.FileAccess.Write))
            //{
            //    byte[] bytes = new byte[imageDataStream.Length];
            //    imageDataStream.Read(bytes, 0, (int)imageDataStream.Length);
            
            //    file.Write(bytes, 0, bytes.Length);
            //    imageDataStream.Close();
            //}
            
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename);
            cloudBlockBlob.Properties.ContentType= "image/jpg";

            await cloudBlockBlob.UploadFromStreamAsync(imageDataStream);
            skyImage.imageData.imageData = "";//erase image from database
            _context.SkyImages.Add(skyImage);
            await _context.SaveChangesAsync();
            //var skyImage2 = await _context.SkyImages.FindAsync(skyImage.skyImageId);
            return CreatedAtAction(nameof(GetSkyImage), new { id = skyImage.skyImageId }, skyImage);
        }

        private   int findImageSet(SkyImage si)
        {
            //gather all images from the last 60 seconds with same camera
            var cutOffDate = (si.date).Subtract(new TimeSpan(0,0,30));
            
            var recentPics = _context.SkyImages.Where(c => c.camera == si.camera && (c.date>cutOffDate))
                .Include(x=>x.detectedObjects)
                .ThenInclude(y=> y.bbox).ToList();

            var meteors = si.detectedObjects.Where(x => x.skyObjectClass == "meteor");


            foreach (SkyImage skyI in recentPics)
            {
                //check if bounding boxes intersect
                foreach (SkyObjectDetection recentdetObj in skyI.detectedObjects)

                {
                    if (recentdetObj.skyObjectClass=="meteor")
                    {
                        Rectangle r1 = new Rectangle(recentdetObj.bbox.xmin, recentdetObj.bbox.ymin, recentdetObj.bbox.xmax - recentdetObj.bbox.xmin, recentdetObj.bbox.ymax - recentdetObj.bbox.ymin);
                        foreach (SkyObjectDetection meteordetObj in meteors)
                        {
                            Rectangle r2 = new Rectangle(meteordetObj.bbox.xmin, meteordetObj.bbox.ymin, meteordetObj.bbox.xmax - meteordetObj.bbox.xmin, meteordetObj.bbox.ymax - meteordetObj.bbox.ymin);
                            if (r1.IntersectsWith(r2))
                            {
                                //intersection found...add to set
                                return skyI.imageSet;
                            }
                        }

                    }
                }
            }
            //no intersections
            int maxCount = 0;
            var maxImage =  _context.SkyImages.OrderByDescending(u => u.imageSet).FirstOrDefault();
            if (maxImage!=null)
                            maxCount = maxImage.imageSet;
            maxCount++;
            return maxCount;
        }

        [HttpGet("reorder/")]
        public async Task<ActionResult> ReorderImageSet()
        {
            //gather all images from the last 60 seconds with same camera
            
            foreach (SkyImage si in _context.SkyImages)
            {
                si.imageSet = -1;
               

            }
            await _context.SaveChangesAsync();
            var allMeteors = _context.SkyImages;


            foreach (SkyImage aMeteor in allMeteors)
            {
                var cutOffDate = (aMeteor.date).Subtract(new TimeSpan(0, 0, 30));

                var recentPics = _context.SkyImages.Where(c => c.camera == aMeteor.camera && (c.date > cutOffDate))
                    .Include(x => x.detectedObjects)
                    .ThenInclude(y => y.bbox).ToList();

                var meteors = aMeteor.detectedObjects.Where(x => x.skyObjectClass == "meteor");
                int maxCount = 0;
                var maxImage = _context.SkyImages.OrderByDescending(u => u.imageSet).FirstOrDefault();
                if (maxImage != null)
                    maxCount = maxImage.imageSet;
                maxCount++;
                aMeteor.imageSet = maxCount;
                foreach (SkyImage skyI in recentPics)
                {
                    //check if bounding boxes intersect
                    foreach (SkyObjectDetection recentdetObj in skyI.detectedObjects)

                    {
                        if (recentdetObj.skyObjectClass == "meteor")
                        {
                            Rectangle r1 = new Rectangle(recentdetObj.bbox.xmin, recentdetObj.bbox.ymin, recentdetObj.bbox.xmax - recentdetObj.bbox.xmin, recentdetObj.bbox.ymax - recentdetObj.bbox.ymin);
                            foreach (SkyObjectDetection meteordetObj in meteors)
                            {
                                Rectangle r2 = new Rectangle(meteordetObj.bbox.xmin, meteordetObj.bbox.ymin, meteordetObj.bbox.xmax - meteordetObj.bbox.xmin, meteordetObj.bbox.ymax - meteordetObj.bbox.ymin);
                                if (r1.IntersectsWith(r2))
                                {
                                    //intersection found...add to set
                                    if (skyI.imageSet>-1)
                                    aMeteor.imageSet= skyI.imageSet;
                                   
                                }
                            }

                        }
                    }
                }
                //no intersections

                _context.Entry(aMeteor).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SkyImageExists(aMeteor.skyImageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                
            }
            return NoContent();
        }

        // DELETE: api/SkyImages/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SkyImage>> DeleteSkyImage(int id)
        {
            var skyImage = await _context.SkyImages
                .Include(c => c.detectedObjects)
                .ThenInclude(si => si.bbox).FirstAsync(y => y.skyImageId == id);
                

            if (skyImage == null)
            {
                return NotFound();
            }
            System.IO.File.Delete(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename));
            _context.SkyImages.Remove(skyImage);
            await _context.SaveChangesAsync();

            return skyImage;
        }

        // DELETE: api/SkyImages
        [HttpDelete]
        public async Task<ActionResult<SkyImage>> DeleteAll()
        {
            foreach (SkyImage si in _context.SkyImages.Include(c => c.detectedObjects)
                .ThenInclude(v => v.bbox))
            {
                System.IO.File.Delete(Path.Combine("\\home\\site\\wwwroot\\images\\", si.filename));
                _context.SkyImages.Remove(si);

            }
            await _context.SaveChangesAsync();

            return null;
        }
        //[HttpPost()]
        //public string UploadFile()
        //{
        //    int iUploadedCnt = 0;

        //    // DEFINE THE PATH WHERE WE WANT TO SAVE THE FILES.
        //    string sPath = "";
        //    sPath = System.Web.Hosting.HostingEnvironment.MapPath("~/locker/");

        //    System.Web.HttpFileCollection hfc = System.Web.HttpContext.Current.Request.Files;

        //    // CHECK THE FILE COUNT.
        //    for (int iCnt = 0; iCnt <= hfc.Count - 1; iCnt++)
        //    {
        //        System.Web.HttpPostedFile hpf = hfc[iCnt];

        //        if (hpf.ContentLength > 0)
        //        {
        //            // CHECK IF THE SELECTED FILE(S) ALREADY EXISTS IN FOLDER. (AVOID DUPLICATE)
        //            if (!File.Exists(sPath + Path.GetFileName(hpf.FileName)))
        //            {
        //                // SAVE THE FILES IN THE FOLDER.
        //                hpf.SaveAs(sPath + Path.GetFileName(hpf.FileName));
        //                iUploadedCnt = iUploadedCnt + 1;
        //            }
        //        }
        //    }

        //    // RETURN A MESSAGE (OPTIONAL).
        //    if (iUploadedCnt > 0)
        //    {
        //        return iUploadedCnt + " Files Uploaded Successfully";
        //    }
        //    else
        //    {
        //        return "Upload Failed";
        //    }
        //}

        private bool SkyImageExists(int id)
        {
            return _context.SkyImages.Any(e => e.skyImageId == id);
        }
    }
}
