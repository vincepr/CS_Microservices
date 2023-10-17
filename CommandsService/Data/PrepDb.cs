using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandsService.Models;
using CommandsService.SyncDataServices.Grpc;

namespace CommandsService.Data;

public static class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder applicationBuilder) {
        // again we cant use constructor-dependency injection because of lifetimes
        // so we use the applicationBuilder to get the serviceScope() to get the Repo we need
        using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope()) {
            var grpcClient = serviceScope.ServiceProvider.GetService<IPlatformDataClient>();
            var platforms = grpcClient!.ReturnAllPlatforms();
            SeedData(serviceScope.ServiceProvider.GetService<ICommandRepo>()!, platforms);
        }
    }

    private static void SeedData(ICommandRepo repo, IEnumerable<Platform> platforms) {
        Console.WriteLine("--> Seeding new platforms");
        foreach (var p in platforms) {
            if (!repo.ExternalPlatformExist(p.ExternalId)) {
                repo.CreatePlatform(p);
            }
        }
        repo.SaveChanges();
    }
}