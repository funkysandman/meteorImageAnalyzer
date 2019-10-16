﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MeteorIngestAPI.Models
{
    public class SkyImage
    {
        [Key]
        public int skyImageId { get; set; }
        public string filename { get; set; }
        public string camera { get; set; }
        public int width { get; set; }
        public int height { get; set; }

        public DateTime date { get; set; }

        public  ICollection<SkyObjectDetection> detectedObjects {get;set;}

        public ImageData imageData { get; set; }

        public int imageSet { get; set; }
    }

    public class ImageData
    {
      
        public string imageData { get; set; }

        [Key]
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("SkyImage")]
        public int skyImageRefId { get; set; }
       // public SkyImage SkyImage { get; set; }
    }
    public class SkyObjectDetection
    {
        [Key]
        public int skyObjectID { get; set; }
        public string skyObjectClass { get; set; }
        public BoundingBox bbox { get; set;}
        public Decimal score { get; set; }
        
    }
    public class BoundingBox
    {
        [Key]
        public int boundingBoxId { get; set; }
        public int xmin { get; set; }
        public int ymin { get; set; }
        public int xmax { get; set; }
        public int ymax { get; set; }


    }

}
