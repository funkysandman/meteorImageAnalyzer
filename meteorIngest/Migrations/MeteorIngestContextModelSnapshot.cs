﻿// <auto-generated />
using System;
using MeteorIngestAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace meteorIngest.Migrations
{
    [DbContext(typeof(MeteorIngestContext))]
    partial class MeteorIngestContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.0.0");

            modelBuilder.Entity("MeteorIngestAPI.Models.BoundingBox", b =>
                {
                    b.Property<int>("boundingBoxId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("xmax")
                        .HasColumnType("INTEGER");

                    b.Property<int>("xmin")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ymax")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ymin")
                        .HasColumnType("INTEGER");

                    b.HasKey("boundingBoxId");

                    b.ToTable("BoundingBox");
                });

            modelBuilder.Entity("MeteorIngestAPI.Models.ImageData", b =>
                {
                    b.Property<int>("skyImageRefId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("imageData")
                        .HasColumnType("TEXT");

                    b.HasKey("skyImageRefId");

                    b.ToTable("ImageData");
                });

            modelBuilder.Entity("MeteorIngestAPI.Models.SkyImage", b =>
                {
                    b.Property<int>("skyImageId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("camera")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("date")
                        .HasColumnType("TEXT");

                    b.Property<string>("filename")
                        .HasColumnType("TEXT");

                    b.Property<int>("height")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("imageDataskyImageRefId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("imageSet")
                        .HasColumnType("INTEGER");

                    b.Property<int>("rank")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("selectedForTraining")
                        .HasColumnType("INTEGER");

                    b.Property<int>("width")
                        .HasColumnType("INTEGER");

                    b.HasKey("skyImageId");

                    b.HasIndex("imageDataskyImageRefId");

                    b.ToTable("SkyImages");
                });

            modelBuilder.Entity("MeteorIngestAPI.Models.SkyObjectDetection", b =>
                {
                    b.Property<int>("skyObjectID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("bboxboundingBoxId")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("score")
                        .HasColumnType("TEXT");

                    b.Property<int?>("skyImageId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("skyObjectClass")
                        .HasColumnType("TEXT");

                    b.HasKey("skyObjectID");

                    b.HasIndex("bboxboundingBoxId");

                    b.HasIndex("skyImageId");

                    b.ToTable("SkyObjectDetection");
                });

            modelBuilder.Entity("MeteorIngestAPI.Models.SkyImage", b =>
                {
                    b.HasOne("MeteorIngestAPI.Models.ImageData", "imageData")
                        .WithMany()
                        .HasForeignKey("imageDataskyImageRefId");
                });

            modelBuilder.Entity("MeteorIngestAPI.Models.SkyObjectDetection", b =>
                {
                    b.HasOne("MeteorIngestAPI.Models.BoundingBox", "bbox")
                        .WithMany()
                        .HasForeignKey("bboxboundingBoxId");

                    b.HasOne("MeteorIngestAPI.Models.SkyImage", null)
                        .WithMany("detectedObjects")
                        .HasForeignKey("skyImageId");
                });
#pragma warning restore 612, 618
        }
    }
}
