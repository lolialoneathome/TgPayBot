﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Sqllite;
using System;

namespace Sqllite.Migrations
{
    [DbContext(typeof(SqlliteDbContext))]
    [Migration("20180414214701_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011");

            modelBuilder.Entity("Sqllite.State", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("IsEnabled");

                    b.HasKey("Id");

                    b.ToTable("States");
                });

            modelBuilder.Entity("Sqllite.UnauthorizedUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ChatId");

                    b.Property<string>("Code");

                    b.Property<string>("PhoneNumber");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.ToTable("UnauthorizedUsers");
                });

            modelBuilder.Entity("Sqllite.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ChatId");

                    b.Property<string>("PhoneNumber");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
