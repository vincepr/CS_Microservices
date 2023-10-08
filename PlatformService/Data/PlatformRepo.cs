using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlatformService.Models;

namespace PlatformService.Data
{
    public class PlatformRepo : IPlatformRepo
    {
        private readonly AppDbContext _ctx;

        public PlatformRepo(AppDbContext ctx)
        {
            _ctx = ctx;   
        }

        /// <summary>
        /// Inserts a new platform into the Database. Will Throw if null is passed in.
        /// </summary>
        /// <param name="plat"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void CreatePlatform(Platform? plat)
        {
            if (plat is null) throw new ArgumentNullException(nameof(plat));
            _ctx.Platforms.Add(plat);
        }

        public Platform? GetPlatformById(int id)
        {
            return _ctx.Platforms.FirstOrDefault(p => p.Id == id);
        }

        public IEnumerable<Platform> GetPlatforms()
        {
            return _ctx.Platforms.ToList();
        }

        public bool SaveChanges()
        {
            return _ctx.SaveChanges() > 0;
        }
    }
}