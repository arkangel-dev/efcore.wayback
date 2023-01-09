﻿// <auto-generated />
using System;
using Sample;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Sample.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20230621125518_AddedAudit")]
    partial class AddedAudit
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.7")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CastleProxiesTest.DbEntities.AuditEntry", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<DateTime>("ChangeDate")
                        .HasColumnType("datetime2");

                    b.Property<int?>("MessageID")
                        .HasColumnType("int");

                    b.Property<string>("NewValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PropertyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TransactionID")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("UserID")
                        .HasColumnType("int");

                    b.HasKey("ID");

                    b.HasIndex("MessageID");

                    b.HasIndex("UserID");

                    b.ToTable("AuditEntries");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.Message", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<string>("Contents")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RecipientID")
                        .HasColumnType("int");

                    b.Property<int>("SenderID")
                        .HasColumnType("int");

                    b.HasKey("ID");

                    b.HasIndex("RecipientID");

                    b.HasIndex("SenderID");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.User", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.AuditEntry", b =>
                {
                    b.HasOne("CastleProxiesTest.DbEntities.Message", null)
                        .WithMany("AuditEntries")
                        .HasForeignKey("MessageID");

                    b.HasOne("CastleProxiesTest.DbEntities.User", null)
                        .WithMany("AuditEntries")
                        .HasForeignKey("UserID");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.Message", b =>
                {
                    b.HasOne("CastleProxiesTest.DbEntities.User", "Recipient")
                        .WithMany("Inbox")
                        .HasForeignKey("RecipientID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CastleProxiesTest.DbEntities.User", "Sender")
                        .WithMany("Sent")
                        .HasForeignKey("SenderID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Recipient");

                    b.Navigation("Sender");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.Message", b =>
                {
                    b.Navigation("AuditEntries");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.User", b =>
                {
                    b.Navigation("AuditEntries");

                    b.Navigation("Inbox");

                    b.Navigation("Sent");
                });
#pragma warning restore 612, 618
        }
    }
}