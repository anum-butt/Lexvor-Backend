using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lexvor.API;
using Lexvor.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Lexvor.Tests
{
    public class BaseTest
    {
        public ApplicationDbContext context { get; set; }
        public OtherSettings other { get; set; }
        public string TestAccountID { get; set; }
        public string Connection { get; set; }

        public BaseTest() {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var config = builder.Build();

            Connection = config.GetConnectionString("DefaultConnection");

            var options =
                new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection);
            context = new ApplicationDbContext(options.Options);

	        other = config.GetSection("OtherSettings").Get<OtherSettings>();
        }
	}
}
