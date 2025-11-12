using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using CoinCraft.Infrastructure;

namespace CoinCraft.Infrastructure.Migrations
{
    [DbContext(typeof(CoinCraftDbContext))]
    public class CoinCraftDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("CoinCraft.Domain.Account", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd();
                b.Property<string>("Nome").IsRequired().HasMaxLength(80);
                b.Property<int>("Tipo");
                b.Property<decimal>("SaldoInicial");
                b.Property<bool>("Ativa");
                b.Property<string>("CorHex");
                b.Property<string>("Icone");
                b.HasKey("Id");
                b.ToTable("Accounts");
            });

            modelBuilder.Entity("CoinCraft.Domain.Category", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd();
                b.Property<string>("Nome").IsRequired().HasMaxLength(80);
                b.Property<string>("CorHex");
                b.Property<string>("Icone");
                b.Property<int?>("ParentCategoryId");
                b.Property<decimal?>("LimiteMensal");
                b.HasKey("Id");
                b.HasIndex("ParentCategoryId");
                b.ToTable("Categories");
            });

            modelBuilder.Entity("CoinCraft.Domain.Goal", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd();
                b.Property<int>("CategoryId");
                b.Property<decimal>("LimiteMensal");
                b.Property<int>("Ano");
                b.Property<int>("Mes");
                b.HasKey("Id");
                b.HasIndex("CategoryId");
                b.ToTable("Goals");
            });

            modelBuilder.Entity("CoinCraft.Domain.UserSetting", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd();
                b.Property<string>("Chave").IsRequired();
                b.Property<string>("Valor").IsRequired();
                b.HasKey("Id");
                b.ToTable("UserSettings");
            });

            modelBuilder.Entity("CoinCraft.Domain.Transaction", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd();
                b.Property<DateTime>("Data");
                b.Property<int>("Tipo");
                b.Property<decimal>("Valor").HasPrecision(18, 2);
                b.Property<int>("AccountId");
                b.Property<int?>("CategoryId");
                b.Property<string>("Descricao");
                b.Property<int?>("OpostoAccountId");
                b.Property<string>("AttachmentPath");
                b.HasKey("Id");
                b.HasIndex("AccountId");
                b.HasIndex("CategoryId");
                b.HasIndex("Data");
                b.HasIndex("OpostoAccountId");
                b.HasIndex("Tipo", "AccountId");
                b.ToTable("Transactions");
            });

            modelBuilder.Entity("CoinCraft.Domain.RecurringTransaction", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd();
                b.Property<string>("Nome").IsRequired().HasMaxLength(200);
                b.Property<int>("Frequencia");
                b.Property<DateTime>("StartDate");
                b.Property<DateTime?>("EndDate");
                b.Property<int?>("DiaDaSemana");
                b.Property<int?>("DiaDoMes");
                b.Property<bool>("AutoLancamento");
                b.Property<DateTime>("NextRunDate");
                b.Property<int>("Tipo");
                b.Property<decimal>("Valor").HasPrecision(18, 2);
                b.Property<int>("AccountId");
                b.Property<int?>("CategoryId");
                b.Property<string>("Descricao");
                b.Property<int?>("OpostoAccountId");
                b.HasKey("Id");
                b.HasIndex("NextRunDate");
                b.HasIndex("AccountId");
                b.HasIndex("CategoryId");
                b.HasIndex("OpostoAccountId");
                b.HasIndex("Frequencia", "AccountId");
                b.ToTable("RecurringTransactions");
            });

            modelBuilder.Entity("CoinCraft.Domain.Category", b =>
            {
                b.HasOne("CoinCraft.Domain.Category", null)
                    .WithMany()
                    .HasForeignKey("ParentCategoryId");
            });

            modelBuilder.Entity("CoinCraft.Domain.Goal", b =>
            {
                b.HasOne("CoinCraft.Domain.Category", null)
                    .WithMany()
                    .HasForeignKey("CategoryId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity("CoinCraft.Domain.Transaction", b =>
            {
                b.HasOne("CoinCraft.Domain.Account", null)
                    .WithMany()
                    .HasForeignKey("AccountId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne("CoinCraft.Domain.Account", null)
                    .WithMany()
                    .HasForeignKey("OpostoAccountId");

                b.HasOne("CoinCraft.Domain.Category", null)
                    .WithMany()
                    .HasForeignKey("CategoryId");
            });

            modelBuilder.Entity("CoinCraft.Domain.RecurringTransaction", b =>
            {
                b.HasOne("CoinCraft.Domain.Account", null)
                    .WithMany()
                    .HasForeignKey("AccountId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne("CoinCraft.Domain.Account", null)
                    .WithMany()
                    .HasForeignKey("OpostoAccountId");

                b.HasOne("CoinCraft.Domain.Category", null)
                    .WithMany()
                    .HasForeignKey("CategoryId");
            });
        }
    }
}