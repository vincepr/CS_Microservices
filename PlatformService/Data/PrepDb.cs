using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlatformService.Models;

namespace PlatformService.Data
{
    public static class PrepDb
    {
        public static void PrepPopulation(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                SeedData(serviceScope.ServiceProvider.GetService<AppDbContext>());
            }
        }

        public static void SeedData(AppDbContext ctx)
        {
            if(!ctx.Platforms.Any()) {
                ctx.Platforms.AddRange(
                    new Platform() {Name="Dot Net", Publisher="Microsoft", Cost="Free"},
                    new Platform() {Name="SQL Server Express", Publisher="Microsoft", Cost="Free"},
                    new Platform() {Name="Kubernetes", Publisher="Cloud Native Computing Foundation", Cost="Free"}
                );
                ctx.SaveChanges();
                Console.WriteLine("---> Seeding Data with some made up Data");
            } else {
                Console.WriteLine("---> Database already has Data. Didn't have to populate the Database with Seed Data.");
            }
        }
    }
}