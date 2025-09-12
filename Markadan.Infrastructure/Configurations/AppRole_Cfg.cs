using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markadan.Infrastructure.Configurations
{
    public class AppRole_Cfg : IEntityTypeConfiguration<AppRole>
    {
        public void Configure(EntityTypeBuilder<AppRole> b)
        {
            // Identity tablosu default 'AspNetRoles'
            b.ToTable("AspNetRoles");
        }
    }
}