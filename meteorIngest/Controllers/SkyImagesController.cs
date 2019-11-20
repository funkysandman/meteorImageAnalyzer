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
using System.Xml.Linq;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;

namespace meteorIngest.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class SkyImagesController : ControllerBase
    {
        private readonly MeteorIngestContext _context;
        private readonly IConfiguration _configuration;
        const bool local = false;
        public SkyImagesController(MeteorIngestContext context, IConfiguration configuration )
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/SkyImages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SkyImage>>> GetSkyImages()
        {

            return await _context.SkyImages.Include(c => c.detectedObjects)
                .ThenInclude(si => si.bbox).OrderBy(x => x.rank)
                .ToListAsync();

            //var imageList = _context.SkyImages
            //        .FromSqlRaw("Select * from SkyImages a order by  (select count(*) from SkyImages c where c.imageSet = a.imageSet),(select max(score) from SkyObjectDetection where skyImageId=a.skyImageId and skyObjectClass=='meteor') desc")
            //        .ToListAsync<SkyImage>();
            //return await imageList;





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

        [HttpGet("load/")]
        public async Task<ActionResult<IEnumerable<SkyImage>>> LoadSkyImages()
        {
            var directory = new DirectoryInfo("c:\\meteorsXML\\meteor_corpus");


            var files = directory.GetFiles("*.xml"); //.Where(file => file.LastWriteTime >= from_date && file.LastWriteTime <= to_date);
            int skyObjectId = 0;
            int resize = 512; //resize to square
            decimal xFactor = 0;
            decimal yFactor = 0;
            int bbId = 0;

            foreach (FileInfo afile in files)
            {
                if (afile != null)
                {
                    SkyImage si = new SkyImage();
                    si.detectedObjects = new List<SkyObjectDetection>();
                    string file = afile.FullName;
                    XElement boxxml = XElement.Load(afile.FullName);
                    foreach (XElement xe in boxxml.Elements())
                        if (xe.Name.ToString().ToLower() == "size")
                        {
                            foreach (XElement xeSize in xe.Elements())
                            {
                                if (xeSize.Name.ToString().ToLower() == "width")
                                {
                                    //assuming size comes before detected objects
                                    si.width = Convert.ToInt16(xeSize.Value);
                                    xFactor = 512 / Convert.ToDecimal(si.width);
                                    si.width = 512;
                                }

                                if (xeSize.Name.ToString().ToLower() == "height")
                                {
                                    si.height = Convert.ToInt16(xeSize.Value);
                                    yFactor = 512 / Convert.ToDecimal(si.height);
                                    si.height = 512;
                                }
                            }
                        }

                    // 
                    foreach (XElement xe in boxxml.Elements())
                    {


                        if (xe.Name.ToString().ToLower() == "filename")
                            si.filename = xe.Value;
                        if (xe.Name.ToString().ToLower() == "camera")
                            si.camera = xe.Value;
                        if (xe.Name.ToString().ToLower() == "date")
                            si.date = DateTime.ParseExact(xe.Value, "MM/dd/yyyy hh:mm:ss tt", null);
                        if (xe.Name == "object")
                        {
                            var newObject = new SkyObjectDetection();
                            skyObjectId = skyObjectId + 1;
                            //newObject.skyObjectID = skyObjectId;

                            foreach (XElement xe2 in xe.Elements())
                            {
                                switch (xe2.Name.ToString())
                                {
                                    case "name":
                                        newObject.skyObjectClass = xe2.Value;
                                        break;
                                    case "score":
                                        newObject.score = Convert.ToDecimal(xe2.Value);
                                        break;
                                    case "bndbox":
                                        var newBBox = new BoundingBox();
                                        bbId++;
                                        foreach (XElement xe3 in xe2.Elements())
                                        {
                                            switch (xe3.Name.ToString())
                                            {

                                                case "xmin":
                                                    newBBox.xmin = Convert.ToInt32((Convert.ToDecimal(int.Parse(xe3.Value)) * xFactor));
                                                    break;
                                                case "xmax":
                                                    newBBox.xmax = Convert.ToInt32((Convert.ToDecimal(int.Parse(xe3.Value)) * xFactor));
                                                    break;
                                                case "ymin":
                                                    newBBox.ymin = Convert.ToInt32((Convert.ToDecimal(int.Parse(xe3.Value)) * yFactor));
                                                    break;
                                                case "ymax":
                                                    newBBox.ymax = Convert.ToInt32((Convert.ToDecimal(int.Parse(xe3.Value)) * yFactor));
                                                    break;
                                                default:

                                                    break;
                                            }
                                        }
                                        //newBBox.boundingBoxId = bbId;
                                        newObject.bbox = newBBox;
                                        break;
                                    default:
                                        break;


                                }



                            }
                            si.detectedObjects.Add(newObject);


                        }

                    }
                    _context.Add(si);
                }

            }

            await _context.SaveChangesAsync();
            return NoContent();
        }




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

            if (local)
            {

                byte[] bytes = System.IO.File.ReadAllBytes(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename));


                string base64String = Convert.ToBase64String(bytes);
                skyImage.imageData.imageData = base64String;
                return skyImage;

            }
            else
            {
                //get image from cloud storage
                string storageConnection = _configuration.GetSection("myStorage").Value;

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

        }

        [HttpGet("fullNext/{rank}")]
        public async Task<ActionResult<SkyImage>> GetSkyImageWithJPGNext(int rank)
        {
            //var skyImage = await _context.SkyImages.FindAsync(id);
            var skyImage = await _context.SkyImages
                .Include(x => x.imageData)
                .Include(c => c.detectedObjects)
                .ThenInclude(si => si.bbox)
                .Where(s => s.rank > rank).OrderBy(b => b.rank).FirstOrDefaultAsync();






            if (skyImage == null)
            {
                return NotFound();
            }

            if (local)
            {

                byte[] bytes = System.IO.File.ReadAllBytes(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename));


                string base64String = Convert.ToBase64String(bytes);
                skyImage.imageData.imageData = base64String;
                return skyImage;

            }
            else
            {
                //get image from cloud storage
                string storageConnection = _configuration.GetSection("myStorage").Value;

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

        }

        [HttpGet("fullPrev/{rank}")]
        public async Task<ActionResult<SkyImage>> GetSkyImageWithJPGPrev(int rank)
        {
            //var skyImage = await _context.SkyImages.FindAsync(id);
            var skyImage = await _context.SkyImages
                .Include(x => x.imageData)
                .Include(c => c.detectedObjects)
                .ThenInclude(si => si.bbox)
                .Where(s => s.rank < rank).OrderByDescending(b => b.rank).FirstOrDefaultAsync();






            if (skyImage == null)
            {
                return NotFound();
            }

            if (local)
            {

                byte[] bytes = System.IO.File.ReadAllBytes(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename));


                string base64String = Convert.ToBase64String(bytes);
                skyImage.imageData.imageData = base64String;
                return skyImage;

            }
            else
            {
                //get image from cloud storage
                string storageConnection = _configuration.GetSection("myStorage").Value;

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
            if (local)
            {
                using (FileStream file = new FileStream(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename), FileMode.Create, System.IO.FileAccess.Write))
                {
                    byte[] bytes = new byte[imageDataStream.Length];
                    imageDataStream.Read(bytes, 0, (int)imageDataStream.Length);

                    file.Write(bytes, 0, bytes.Length);
                    imageDataStream.Close();
                }
            }
            else
            {
                //

                //save file to azure storage account
                //string storageConnection = CloudConfigurationManager.GetSetting("BlobStorageConnectionString");
                string storageConnection = _configuration.GetSection("myStorage").Value;
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);

                //create a block blob 
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                //create a container 
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");

                //create a container if it is not already exists

                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {

                    await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                }





                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename);
                cloudBlockBlob.Properties.ContentType = "image/jpg";

                await cloudBlockBlob.UploadFromStreamAsync(imageDataStream);
                //
                //generate xml annotation file
            }
            XElement xmlTree = new XElement("annotation");


            XElement aFilename = new XElement("filename");

            aFilename.Value = skyImage.filename;
            xmlTree.Add(aFilename);
            XElement aSize = new XElement("size");
            aSize.Add(new XElement("width", skyImage.width));
            aSize.Add(new XElement("height", skyImage.height));
            aSize.Add(new XElement("depth", "1"));
            xmlTree.Add(aSize);

            foreach (SkyObjectDetection sod in skyImage.detectedObjects)
            {
                XElement anObject = new XElement("object");
                anObject.Add(new XElement("score", sod.score));
                anObject.Add(new XElement("name", sod.skyObjectClass));
                XElement bndBox = new XElement("bndbox");



                bndBox.Add(new XElement("xmin", sod.bbox.xmin));
                bndBox.Add(new XElement("ymin", sod.bbox.ymin));
                bndBox.Add(new XElement("xmax", sod.bbox.xmax));
                bndBox.Add(new XElement("ymax", sod.bbox.ymax));
                anObject.Add(bndBox);
                xmlTree.Add(anObject);


            }





            xmlTree.FirstNode.AddAfterSelf(new XElement("camera", skyImage.camera));
            xmlTree.FirstNode.AddAfterSelf(new XElement("dateTaken", skyImage.date));


            //cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename.Replace(".jpg",".xml"));
            //cloudBlockBlob.Properties.ContentType = "text/xml";
            ////updload xml
            //await cloudBlockBlob.UploadTextAsync(xmlTree.ToString());


            //
            skyImage.imageData.imageData = "";//erase image from database
            _context.SkyImages.Add(skyImage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSkyImage), new { id = skyImage.skyImageId }, skyImage);
        }

        private int findImageSet(SkyImage si)
        {
            //gather all images from the last 60 seconds with same camera
            var cutOffDate = (si.date).Subtract(new TimeSpan(0, 0, 30));

            var recentPics = _context.SkyImages.Where(c => c.camera == si.camera && (c.date > cutOffDate))
                .Include(x => x.detectedObjects)
                .ThenInclude(y => y.bbox).ToList();

            var meteors = si.detectedObjects.Where(x => x.skyObjectClass == "meteor");


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
                                return skyI.imageSet;
                            }
                        }

                    }
                }
            }
            //no intersections
            int maxCount = 0;
            var maxImage = _context.SkyImages.OrderByDescending(u => u.imageSet).FirstOrDefault();
            if (maxImage != null)
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
            var allMeteors = _context.SkyImages.Include(x => x.detectedObjects)
                .ThenInclude(y => y.bbox).OrderBy(y => y.camera).OrderBy(x => x.date);
            int maxCount = 1;
            SkyImage anImage = new SkyImage();
            anImage.imageSet = 0;
            anImage.camera = "";

            foreach (SkyImage aMeteor in allMeteors)
            {
                aMeteor.imageSet = maxCount;
                DateTime minDate = aMeteor.date - new TimeSpan(0, 0, 15);
                DateTime maxDate = aMeteor.date + new TimeSpan(0, 0, 15);
                var allMeteors2 = _context.SkyImages.Where(z => z.date > minDate && z.date < maxDate && z.camera == aMeteor.camera).Include(x => x.detectedObjects)
                .ThenInclude(y => y.bbox).OrderBy(y => y.camera).OrderBy(x => x.date);
                foreach (SkyImage aMeteor2 in allMeteors2)

                {
                    var meteors1 = aMeteor.detectedObjects.Where(x => x.skyObjectClass.Contains("meteor"));
                    var meteors2 = aMeteor2.detectedObjects.Where(x => x.skyObjectClass.Contains("meteor"));
                    foreach (SkyObjectDetection meteordetObj1 in meteors1)
                    {
                        foreach (SkyObjectDetection meteordetObj2 in meteors2)
                        {
                            Rectangle r1 = new Rectangle(meteordetObj1.bbox.xmin, meteordetObj1.bbox.ymin, meteordetObj1.bbox.xmax - meteordetObj1.bbox.xmin, meteordetObj1.bbox.ymax - meteordetObj1.bbox.ymin);
                            Rectangle r2 = new Rectangle(meteordetObj2.bbox.xmin, meteordetObj2.bbox.ymin, meteordetObj1.bbox.xmax - meteordetObj2.bbox.xmin, meteordetObj2.bbox.ymax - meteordetObj1.bbox.ymin);
                            if (r1.IntersectsWith(r2))
                            {
                                //probably the same object
                                if (aMeteor2.imageSet > 0)
                                {
                                    aMeteor.imageSet = aMeteor2.imageSet;
                                }
                                else
                                {
                                    aMeteor2.imageSet = aMeteor.imageSet;
                                }
                            }
                        }
                    }


                    // Rectangle r1 = new Rectangle(aMeteor.bbox.xmin, recentdetObj.bbox.ymin, recentdetObj.bbox.xmax - recentdetObj.bbox.xmin, recentdetObj.bbox.ymax - recentdetObj.bbox.ymin);

                }
                //await _context.SaveChangesAsync();
                maxCount++;
            }


            //assign rank
            await _context.SaveChangesAsync();
            int rank = 1;
            var imageList = _context.SkyImages
                    .FromSqlRaw("Select * from SkyImages a order by  (select count(*) from SkyImages c where c.imageSet = a.imageSet),(select max(score) from SkyObjectDetection where skyImageId=a.skyImageId and skyObjectClass=='meteor') desc")
                    .ToList<SkyImage>();
            foreach (SkyImage skyI in imageList)
            {
                skyI.rank = rank;
                rank++;
            }

            await _context.SaveChangesAsync();
            //no intersections



            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

                throw;

            }



            return NoContent();
        }
        [HttpGet("generateXML/")]
        public async Task<ActionResult> GenerateXML()
        {

            string storageConnection = _configuration.GetSection("myStorage").Value;
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);

            //create a block blob 
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");
            var allMeteors = _context.SkyImages.Where(y => y.selectedForTraining == true).Include(x => x.detectedObjects)
                .ThenInclude(y => y.bbox);


            foreach (SkyImage skyImage in allMeteors)
            {
                //generate xml annotation file
                XElement xmlTree = new XElement("annotation");


                XElement aFilename = new XElement("filename");

                aFilename.Value = skyImage.filename;
                xmlTree.Add(aFilename);
                XElement aSize = new XElement("size");
                aSize.Add(new XElement("width", skyImage.width));
                aSize.Add(new XElement("height", skyImage.height));
                aSize.Add(new XElement("depth", "1"));
                xmlTree.Add(aSize);

                foreach (SkyObjectDetection sod in skyImage.detectedObjects)
                {
                    XElement anObject = new XElement("object");
                    anObject.Add(new XElement("score", sod.score));
                    anObject.Add(new XElement("name", sod.skyObjectClass));
                    XElement bndBox = new XElement("bndbox");



                    bndBox.Add(new XElement("xmin", sod.bbox.xmin));
                    bndBox.Add(new XElement("ymin", sod.bbox.ymin));
                    bndBox.Add(new XElement("xmax", sod.bbox.xmax));
                    bndBox.Add(new XElement("ymax", sod.bbox.ymax));
                    anObject.Add(bndBox);
                    xmlTree.Add(anObject);


                }





                xmlTree.FirstNode.AddAfterSelf(new XElement("camera", skyImage.camera));
                xmlTree.FirstNode.AddAfterSelf(new XElement("dateTaken", skyImage.date));


                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename.Replace(".jpg", ".xml"));
                cloudBlockBlob.Properties.ContentType = "text/xml";
                //updload xml
                await cloudBlockBlob.UploadTextAsync(xmlTree.ToString());

            }


            return NoContent();
        }
        [HttpGet("generateXMLlocal/")]
        public async Task<ActionResult> GenerateXMLlocal()
        {


            var allMeteors = _context.SkyImages.Include(x => x.detectedObjects)
                .ThenInclude(y => y.bbox);


            foreach (SkyImage skyImage in allMeteors)
            {
                //generate xml annotation file
                XElement xmlTree = new XElement("annotation");


                XElement aFilename = new XElement("filename");

                aFilename.Value = skyImage.filename;
                xmlTree.Add(aFilename);
                XElement aSize = new XElement("size");
                aSize.Add(new XElement("width", skyImage.width));
                aSize.Add(new XElement("height", skyImage.height));
                aSize.Add(new XElement("depth", "1"));
                xmlTree.Add(aSize);

                foreach (SkyObjectDetection sod in skyImage.detectedObjects)
                {
                    XElement anObject = new XElement("object");
                    anObject.Add(new XElement("score", sod.score));
                    anObject.Add(new XElement("name", sod.skyObjectClass));
                    XElement bndBox = new XElement("bndbox");



                    bndBox.Add(new XElement("xmin", sod.bbox.xmin));
                    bndBox.Add(new XElement("ymin", sod.bbox.ymin));
                    bndBox.Add(new XElement("xmax", sod.bbox.xmax));
                    bndBox.Add(new XElement("ymax", sod.bbox.ymax));
                    anObject.Add(bndBox);
                    xmlTree.Add(anObject);


                }





                xmlTree.FirstNode.AddAfterSelf(new XElement("camera", skyImage.camera));
                xmlTree.FirstNode.AddAfterSelf(new XElement("dateTaken", skyImage.date));

                xmlTree.Save("\\home\\site\\wwwroot\\images\\" + skyImage.filename.Replace(".jpg", ".xml"));

            }


            return NoContent();
        }
        //[HttpGet("generateXML/")]
        //public async Task<ActionResult> GenerateZip()
        //{
        //    //gather all images from the last 60 seconds with same camera
        //    CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);

        //    //create a block blob 
        //    CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
        //    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");


        //    string zipName = "images.zip";

        //        using (ZipArchive newFile = ZipFile.Open(zipName, ZipArchiveMode.Create))
        //        {
        //        var blobs = cloudBlobContainer.ListBlobsSegmentedAsync();

        //        foreach (var blobItem in blobs)
        //        {
        //            Console.WriteLine(blobItem.Uri);
        //        }
        //        {
        //                newFile.CreateEntryFromFile(file, System.IO.Path.GetFileName(file));
        //            }
        //        }





        //    return NoContent();
        //}
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
            if (local)
            {
                System.IO.File.Delete(Path.Combine("\\home\\site\\wwwroot\\images\\", skyImage.filename));
            }
            else
            {
                //string storageConnection = CloudConfigurationManager.GetSetting("BlobStorageConnectionString");
                string storageConnection = "DefaultEndpointsProtocol=https;AccountName=meteorshots;AccountKey=M+rGNU1Ija+Zrs09fVL8FiVj+HVWkx1ji4MvRcSC0Yaa/G+A+MOdN3rAWWCMu8pLBBrFfxM8K4d68FBbsTOmYw==;EndpointSuffix=core.windows.net";
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);

                //create a block blob 
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                //create a container 
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename);
                await cloudBlockBlob.DeleteIfExistsAsync();
                cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(skyImage.filename.Replace(".jpg", ".xml"));
                await cloudBlockBlob.DeleteIfExistsAsync();
            }
          
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
                if (local)
                {
                    try
                    {
                        System.IO.File.Delete(Path.Combine("\\home\\site\\wwwroot\\images\\", si.filename));
                    }
                    catch
                    { }

                }
                else
                {
                    string storageConnection = "DefaultEndpointsProtocol=https;AccountName=meteorshots;AccountKey=M+rGNU1Ija+Zrs09fVL8FiVj+HVWkx1ji4MvRcSC0Yaa/G+A+MOdN3rAWWCMu8pLBBrFfxM8K4d68FBbsTOmYw==;EndpointSuffix=core.windows.net";
                    CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);

                    //create a block blob 
                    CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

                    //create a container 
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(si.filename);
                    await cloudBlockBlob.DeleteIfExistsAsync();
                    cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(si.filename.Replace(".jpg", ".xml"));
                    await cloudBlockBlob.DeleteIfExistsAsync();
                }
                _context.SkyImages.Remove(si);

            }
            await _context.SaveChangesAsync();

            return null;
        }

        // DELETE: api/SkyImages
        [HttpDelete("deleteUnselected/")]
        public async Task<ActionResult<SkyImage>> DeleteUnSelected()
        {

            string storageConnection = _configuration.GetSection("myStorage").Value;
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnection);

            //create a block blob 
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            //create a container 
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("found");


            foreach (SkyImage si in _context.SkyImages.Where(x => x.selectedForTraining == false).Include(c => c.detectedObjects)
                .ThenInclude(v => v.bbox))
            {
                if (local)
                {
                    try
                    {
                        System.IO.File.Delete(Path.Combine("\\home\\site\\wwwroot\\images\\", si.filename));
                    }
                    catch
                    { }

                }
                else
                {


                    //create a container 

                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(si.filename);
                    await cloudBlockBlob.DeleteIfExistsAsync();
                    cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(si.filename.Replace(".jpg", ".xml"));
                    await cloudBlockBlob.DeleteIfExistsAsync();
                }
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
        //private string getConnectionString()
        //{
        //    var kvClient = new KeyVaultClient(AuthenticateVault);
        //    var connectStr = ConfigurationManager.ConnectionStrings["asdasd"].ConnectionString;

        //    return connectStr;

        //}
        //private Task<string> AuthenticateVault(string authority, string resource, string scope)
        //{
        //    var clientCredential = new ClientCredential("da6fd76b-6d1a-4b33-80ca-144e076771b0", "");

        //}
    }
}
