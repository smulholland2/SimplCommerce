﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using SimplCommerce.Core.ApplicationServices;
using SimplCommerce.Core.Domain.Models;
using SimplCommerce.Infrastructure;
using SimplCommerce.Infrastructure.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SimplCommerce.Core.Infrastructure.EntityFramework
{
    public class HvDbContext : IdentityDbContext<User, Role, long>
    {
        public HvDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            List<Type> typeToRegisters = new List<Type>();
            foreach(var module in GlobalConfiguration.Modules)
            {
                var assembly = Assembly.Load(new AssemblyName(module.AssemblyName));
                typeToRegisters.AddRange(assembly.DefinedTypes.Select(t => t.AsType()));
            }

            RegisterEntities(modelBuilder, typeToRegisters);

            RegiserConvention(modelBuilder);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
            .ToTable("Core_User");

            modelBuilder.Entity<Role>()
                .ToTable("Core_Role");

            modelBuilder.Entity<IdentityUserClaim<long>>(b =>
            {
                b.HasKey(uc => uc.Id);
                b.ToTable("Core_UserClaim");
            });

            modelBuilder.Entity<IdentityRoleClaim<long>>(b =>
            {
                b.HasKey(rc => rc.Id);
                b.ToTable("Core_RoleClaim");
            });

            modelBuilder.Entity<IdentityUserRole<long>>(b =>
            {
                b.HasKey(r => new { r.UserId, r.RoleId });
                b.ToTable("Core_UserRole");
            });

            modelBuilder.Entity<IdentityUserLogin<long>>(b =>
            {
                b.ToTable("Core_UserLogin");
            });

            //modelBuilder.Entity<User>(u =>
            //{
            //    u.HasOne(x => x.CurrentShippingAddress).WithMany()
            //   .HasForeignKey(x => x.CurrentShippingAddressId)
            //   .WillCascadeOnDelete(false);
            //});

            modelBuilder.Entity<ProductTemplateProductAttribute>()
                .HasKey(t => new { t.TemplateId, t.AttributeId });

            modelBuilder.Entity<ProductTemplateProductAttribute>()
                .HasOne(pt => pt.Template)
                .WithMany(p => p.ProductAttributes)
                .HasForeignKey(pt => pt.TemplateId);

            modelBuilder.Entity<ProductTemplateProductAttribute>()
                .HasOne(pt => pt.Attribute)
                .WithMany(t => t.ProductTemplates)
                .HasForeignKey(pt => pt.AttributeId);

            modelBuilder.Entity<Address>(x =>
            {
                x.HasOne(d => d.District)
                   .WithMany()
                   .OnDelete(DeleteBehavior.Restrict);

                x.HasOne(d => d.StateOrProvince)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);

                x.HasOne(d => d.Country)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void RegiserConvention(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.ClrType.Namespace != null)
                {
                    var nameParts = entity.ClrType.Namespace.Split('.');
                    var tableName = string.Concat(nameParts[1], "_", entity.ClrType.Name);
                    modelBuilder.Entity(entity.Name).ToTable(tableName);
                }
            }
        }

        private static void RegisterEntities(ModelBuilder modelBuilder, IEnumerable<Type> typeToRegisters)
        {
            var entityTypes = typeToRegisters.Where(x => x.GetTypeInfo().IsSubclassOf(typeof(Entity)) && !x.GetTypeInfo().IsAbstract);
            foreach (var type in entityTypes)
            {
                modelBuilder.Entity(type);
            }
        }
    }
}