using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CSVPrint.Models;

namespace CSVPrint.Data
{
    public class CSVPrintContext : DbContext
    {
        public CSVPrintContext (DbContextOptions<CSVPrintContext> options)
            : base(options)
        {
        }

        public DbSet<CSVPrint.Models.Movies> Movies { get; set; }
    }
}
