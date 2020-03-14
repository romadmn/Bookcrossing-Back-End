﻿using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infastructure.Configuration
{
    class AuthorConfiguration : IEntityTypeConfiguration<Author>
    {
        public void Configure(EntityTypeBuilder<Author> builder)
        {
            builder.ToTable("Author");
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(e => e.FirstName)
                .IsRequired()
                .HasColumnName("firstname")
                .HasMaxLength(20);

            builder.Property(e => e.LastName)
                .IsRequired()
                .HasColumnName("lastname")
                .HasMaxLength(20);

            builder.Property(e => e.MiddleName)
                .HasColumnName("middlename")
                .HasMaxLength(30);
        }       
    }
}