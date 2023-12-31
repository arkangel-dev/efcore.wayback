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
    [Migration("20230706072104_ChangedDBName")]
    partial class ChangedDBName
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

            modelBuilder.Entity("CastleProxiesTest.DbEntities.Interest", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<string>("InterestName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.ToTable("Interests");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.JUser_Interest", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<int>("InterestID")
                        .HasColumnType("int");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("ID");

                    b.HasIndex("InterestID");

                    b.HasIndex("UserID");

                    b.ToTable("Junction_Interests_Users");
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

                    b.Property<int?>("RecipientID")
                        .HasColumnType("int");

                    b.Property<int?>("SenderID")
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

                    b.Property<int?>("BestFriendID")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.HasIndex("BestFriendID");

                    b.ToTable("UserL");
                });

            modelBuilder.Entity("WaybackMachine.Entities.AuditRecord", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<int>("ChangeType")
                        .IsUnicode(false)
                        .HasColumnType("int");

                    b.Property<int>("EntityID")
                        .HasColumnType("int");

                    b.Property<int?>("J1")
                        .HasColumnType("int");

                    b.Property<string>("J1Table")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("J2")
                        .HasColumnType("int");

                    b.Property<string>("J2Table")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NewValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OldValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ParentTransactionID")
                        .HasColumnType("int");

                    b.Property<string>("PropertyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TableName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ID");

                    b.HasIndex("ParentTransactionID");

                    b.ToTable("AuditEntries");
                });

            modelBuilder.Entity("WaybackMachine.Entities.AuditTransactionRecord", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ID"));

                    b.Property<DateTime>("ChangeDate")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("TransactionID")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("ID");

                    b.ToTable("AuditTransactions");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.JUser_Interest", b =>
                {
                    b.HasOne("CastleProxiesTest.DbEntities.Interest", "Interest")
                        .WithMany()
                        .HasForeignKey("InterestID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CastleProxiesTest.DbEntities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Interest");

                    b.Navigation("User");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.Message", b =>
                {
                    b.HasOne("CastleProxiesTest.DbEntities.User", "Recipient")
                        .WithMany("Inbox")
                        .HasForeignKey("RecipientID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("CastleProxiesTest.DbEntities.User", "Sender")
                        .WithMany("Sent")
                        .HasForeignKey("SenderID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Recipient");

                    b.Navigation("Sender");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.User", b =>
                {
                    b.HasOne("CastleProxiesTest.DbEntities.User", "BestFriend")
                        .WithMany()
                        .HasForeignKey("BestFriendID");

                    b.Navigation("BestFriend");
                });

            modelBuilder.Entity("WaybackMachine.Entities.AuditRecord", b =>
                {
                    b.HasOne("WaybackMachine.Entities.AuditTransactionRecord", "ParentTransaction")
                        .WithMany("Changes")
                        .HasForeignKey("ParentTransactionID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentTransaction");
                });

            modelBuilder.Entity("CastleProxiesTest.DbEntities.User", b =>
                {
                    b.Navigation("Inbox");

                    b.Navigation("Sent");
                });

            modelBuilder.Entity("WaybackMachine.Entities.AuditTransactionRecord", b =>
                {
                    b.Navigation("Changes");
                });
#pragma warning restore 612, 618
        }
    }
}
