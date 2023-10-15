using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlatformService.Models;

namespace PlatformService.Data
{
    public static class PrepDb
    {
        public static void PrepPopulation(IApplicationBuilder app, bool isProduction)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                SeedData(serviceScope.ServiceProvider.GetService<AppDbContext>(), isProduction);
            }
        }

        public static void SeedData(AppDbContext ctx, bool isProduction)
        {
            // we want to migrate our migrations when against the real DB:
            if(isProduction) {
                Console.WriteLine("--> Attempting to apply migrations.. ");
                try {
                    ctx.Database.Migrate();
                } catch(Exception e) {
                    Console.WriteLine($"--> Failed to run migrations! {e.Message}");
                }
            }

            if(!ctx.Platforms.Any()) {
                Console.WriteLine("--> Seeding Data with some made up Data");
                ctx.Platforms.AddRange(
                    new Platform() {Name="Dot Net", Publisher="Microsoft", Cost="Free"},
                    new Platform() {Name="SQL Server Express", Publisher="Microsoft", Cost="Free"},
                    new Platform() {Name="Kubernetes", Publisher="Cloud Native Computing Foundation", Cost="Free"}
                );
                ctx.SaveChanges();
            } else {
                Console.WriteLine("--> Database already has Data. Didn't have to populate the Database with Seed Data.");
            }
        }
    }
}