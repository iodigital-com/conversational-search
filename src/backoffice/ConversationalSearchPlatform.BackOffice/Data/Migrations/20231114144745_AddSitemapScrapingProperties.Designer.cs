﻿// <auto-generated />
using System;
using ConversationalSearchPlatform.BackOffice.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ConversationalSearchPlatform.BackOffice.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20231114144745_AddSitemapScrapingProperties")]
    partial class AddSitemapScrapingProperties
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0-rc.2.23480.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ConversationalSearchPlatform.BackOffice.Data.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers", (string)null);

                    b.HasData(
                        new
                        {
                            Id = "68657A77-57AE-409D-A845-5ABAF7C1E633",
                            AccessFailedCount = 0,
                            ConcurrencyStamp = "45c28633-2afa-41a4-a0d7-c80734f10fd6",
                            Email = "user@test.com",
                            EmailConfirmed = true,
                            LockoutEnabled = false,
                            NormalizedEmail = "USER@POLESTAR.COM",
                            NormalizedUserName = "polestaruser",
                            PasswordHash = "AQAAAAIAAYagAAAAEBAdZ4MF0qC+TokeGXpKt7ssgTsUDlCc+MxsPu37URQPSPuUfvpo2T2eqJ4t+jFSuw==",
                            PhoneNumberConfirmed = false,
                            SecurityStamp = "3fd24e71-41a2-4d4c-b7b8-7c99e07fe66e",
                            TenantId = "D2FA78CE-3185-458E-964F-8FD0052B4330",
                            TwoFactorEnabled = false,
                            UserName = "user@polestar.com"
                        },
                        new
                        {
                            Id = "8D4540D4-D50F-48D0-9508-503883712B1A",
                            AccessFailedCount = 0,
                            ConcurrencyStamp = "796bc471-5132-43c7-95f7-8a69325141f0",
                            Email = "user2@test.com",
                            EmailConfirmed = true,
                            LockoutEnabled = false,
                            NormalizedEmail = "USER2@TEST.COM",
                            NormalizedUserName = "USER2",
                            PasswordHash = "AQAAAAIAAYagAAAAEM1CbMbIMqk/SxgbQ1M7FsV8Ombxu+8B2cYzaI8arDdZeshxC5CdiXrIIJO4wIyZRw==",
                            PhoneNumberConfirmed = false,
                            SecurityStamp = "3e4d3e39-7ab3-4eb4-9717-a4a3460c62a6",
                            TenantId = "D2FA78CE-3185-458E-964F-8FD0052B4330",
                            TwoFactorEnabled = false,
                            UserName = "user2"
                        },
                        new
                        {
                            Id = "61581AFC-FC42-41BF-A483-F9863B8E4693",
                            AccessFailedCount = 0,
                            ConcurrencyStamp = "aed18559-b9e4-4797-9b9b-a0a1d87b518c",
                            Email = "admin@test.com",
                            EmailConfirmed = true,
                            LockoutEnabled = false,
                            NormalizedEmail = "ADMIN@TEST.com",
                            NormalizedUserName = "admin",
                            PasswordHash = "AQAAAAIAAYagAAAAEMVbxAqRNlKX/mrEqeZkZkcYzIc8QE+DMCnKcTw3akshy7N7w6+YQlZ0nxqCUxDDng==",
                            PhoneNumberConfirmed = false,
                            SecurityStamp = "ce8b4bb5-acd5-484f-ab07-991e0f050a5d",
                            TenantId = "CCFA9314-ABE6-403A-9E21-2B31D95A5258",
                            TwoFactorEnabled = false,
                            UserName = "admin"
                        },
                        new
                        {
                            Id = "DBC834A9-2561-4381-BADA-15CF89F0F8A8",
                            AccessFailedCount = 0,
                            ConcurrencyStamp = "36aeeb61-f404-4f89-b6be-efe2739f690f",
                            Email = "demo@iodigital.com",
                            EmailConfirmed = true,
                            LockoutEnabled = false,
                            NormalizedEmail = "DEMO@IODIGITAL.COM",
                            NormalizedUserName = "IODIGITALDEMO",
                            PasswordHash = "AQAAAAIAAYagAAAAEG06GZXUu3klozN6yRogxqFvvclyD3Twae9ED7K8hunPIzuUy3eFkL4qUnfF7zW9wA==",
                            PhoneNumberConfirmed = false,
                            SecurityStamp = "7236bd60-c489-4578-b708-40b3d5024919",
                            TenantId = "4903E29F-D633-4A4C-9065-FE3DD8F27E40",
                            TwoFactorEnabled = false,
                            UserName = "iodigitalDemo"
                        },
                        new
                        {
                            Id = "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38",
                            AccessFailedCount = 0,
                            ConcurrencyStamp = "34f698c6-4630-4aaf-b733-b676109e2230",
                            Email = "demoadmin@iodigital.com",
                            EmailConfirmed = true,
                            LockoutEnabled = false,
                            NormalizedEmail = "DEMOADMIN@IODIGITAL.COM",
                            NormalizedUserName = "IODIGITALDEMOADMIN",
                            PasswordHash = "AQAAAAIAAYagAAAAEPtS8pLOe2TTrWRtBP/VsYRCEWyf5Jx7sCdJnJzxP6Ivtw6EYk28ikxdyRRmhEf3Zg==",
                            PhoneNumberConfirmed = false,
                            SecurityStamp = "59d35ab5-61c9-414e-912d-e812b2f8fee4",
                            TenantId = "4903E29F-D633-4A4C-9065-FE3DD8F27E40",
                            TwoFactorEnabled = false,
                            UserName = "iodigitalDemoAdmin"
                        });
                });

            modelBuilder.Entity("ConversationalSearchPlatform.BackOffice.Data.Entities.UserInvite", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("CreatedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("ExpirationDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("MailSentDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("RedeemDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("TenantId");

                    b.ToTable("UserInvites");
                });

            modelBuilder.Entity("ConversationalSearchPlatform.BackOffice.Data.Entities.WebsitePage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("IndexableType")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("IndexedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("IsSitemapParent")
                        .HasColumnType("bit");

                    b.Property<int>("Language")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("ReferenceType")
                        .HasColumnType("int");

                    b.Property<string>("SitemapFileName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SitemapFileReference")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("Url")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.HasIndex("ParentId");

                    b.HasIndex("TenantId");

                    b.ToTable("WebsitePages");

                    b.HasAnnotation("Finbuckle:MultiTenant", true);
                });

            modelBuilder.Entity("ConversationalSearchPlatform.BackOffice.Data.OpenAIConsumption", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("CallModel")
                        .HasColumnType("int");

                    b.Property<int>("CallType")
                        .HasColumnType("int");

                    b.Property<decimal>("CompletionTokenCost")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("CompletionTokens")
                        .HasColumnType("int");

                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("ExecutedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("PromptTokenCost")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("PromptTokens")
                        .HasColumnType("int");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<decimal>("ThousandUnitsCompletionCost")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<decimal>("ThousandUnitsPromptCost")
                        .HasPrecision(18, 8)
                        .HasColumnType("decimal(18,8)");

                    b.Property<int>("UsageType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CorrelationId");

                    b.HasIndex("ExecutedAt");

                    b.HasIndex("TenantId");

                    b.ToTable("OpenAiConsumptions");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles", (string)null);

                    b.HasData(
                        new
                        {
                            Id = "E71D0DC1-4121-4E0B-9F71-F90949029688",
                            Name = "Administrator",
                            NormalizedName = "ADMINISTRATOR"
                        },
                        new
                        {
                            Id = "69FD93B6-C1D1-43C1-A2E9-31C02084EEB6",
                            Name = "User",
                            NormalizedName = "USER"
                        },
                        new
                        {
                            Id = "0AD168F8-45F8-441C-878A-E14B8F019229",
                            Name = "Readonly",
                            NormalizedName = "READONLY"
                        });
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);

                    b.HasData(
                        new
                        {
                            UserId = "68657A77-57AE-409D-A845-5ABAF7C1E633",
                            RoleId = "69FD93B6-C1D1-43C1-A2E9-31C02084EEB6"
                        },
                        new
                        {
                            UserId = "8D4540D4-D50F-48D0-9508-503883712B1A",
                            RoleId = "69FD93B6-C1D1-43C1-A2E9-31C02084EEB6"
                        },
                        new
                        {
                            UserId = "61581AFC-FC42-41BF-A483-F9863B8E4693",
                            RoleId = "E71D0DC1-4121-4E0B-9F71-F90949029688"
                        },
                        new
                        {
                            UserId = "01F243C2-C08C-412F-B2C0-EAB2BCEB4C38",
                            RoleId = "E71D0DC1-4121-4E0B-9F71-F90949029688"
                        },
                        new
                        {
                            UserId = "DBC834A9-2561-4381-BADA-15CF89F0F8A8",
                            RoleId = "0AD168F8-45F8-441C-878A-E14B8F019229"
                        });
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("ConversationalSearchPlatform.BackOffice.Data.Entities.WebsitePage", b =>
                {
                    b.HasOne("ConversationalSearchPlatform.BackOffice.Data.Entities.WebsitePage", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("ConversationalSearchPlatform.BackOffice.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("ConversationalSearchPlatform.BackOffice.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ConversationalSearchPlatform.BackOffice.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("ConversationalSearchPlatform.BackOffice.Data.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
