# Platform Service
part of the https://github.com/vincepr/CS_Microservices Project

## setup
```
dotnet new webapi -n PlatformService

dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

## Adding Automapper
- `Program.cs`
```csharp
// we inject Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
```

Then we create Profiles that Map our Models together with our Dtos
- `Profiles/PlatformsProfile.cs`
```csharp
using AutoMapper;
using PlatformService.Dtos;
using PlatformService.Models;

namespace PlatformService.Profiles;

public class PlatformsProfile : Profile {
    public PlatformsProfile() {

        // Because Names of attributes match 1:1 this is all it needs
        //       <from Source, to Target>
        CreateMap<Platform, PlatformReadDto>(); 
        CreateMap<PlatformCreateDto, Platform>();
    }
}
```

## Our Controller Endpoint
- `Controllers/PlatformsController.cs`
```csharp
namespace PlatformService.Controllers;

[Route("api/[controller]")]     // translates to ".../api/Platforms/"
[ApiController] // implements some default behaviours we want from our Controller
public class PlatformsController: ControllerBase {
    private readonly IPlatformRepo _repository;
    private readonly IMapper _mapper;

    public PlatformsController(IPlatformRepo repository, IMapper mapper) {
        _repository = repository;
        _mapper = mapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms() {
        var platformItems = _repository.GetPlatforms();
        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id) {
        var platform = _repository.GetPlatformById(id);
        if (platform is null) return NotFound();
        return Ok(_mapper.Map<PlatformReadDto>(platform));
    }

    [HttpPost]
    public ActionResult<PlatformReadDto> CreatePlatform(PlatformCreateDto dto) {
        var newPlatform = _mapper.Map<Platform>(dto);
        _repository.CreatePlatform(newPlatform);
        if (_repository.SaveChanges() == false) return StatusCode(500);
         
        var platformReadDto = _mapper.Map<PlatformReadDto>(newPlatform);

        return CreatedAtRoute(
            nameof(GetPlatformById),            // provides a link to the /api/get/{newId}
            new { Id = platformReadDto.Id },    // the id of our newly created obj
            platformReadDto);                   // and we also return the dtoObject directly
    }
}
```

## Data related things
Our Actual Database Implemention, at the start a Imemory, later Ms-SqlServer
- `Data/AppDbContext.cs`
```csharp
namespace PlatformService.Data {
    public class AppDbContext : DbContext {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }   
        public DbSet<Platform> Platforms { get; set; }
    }
}
```

This Interface Makes Swapping How We implement Our Repository Methods easy. (also testable)
- `Data/IPlatformRepo.cs`
```csharp
namespace PlatformService.Data {
    public interface IPlatformRepo
    {
        bool SaveChanges();

        IEnumerable<Platform> GetPlatforms();

        Platform? GetPlatformById(int id);

        void CreatePlatform(Platform? plat);
    }
}
```

Our Implementation of our Repository Methods.
- `Data/PlatformRepo.cs`
```csharp
namespace PlatformService.Data {
    public class PlatformRepo : IPlatformRepo {
        private readonly AppDbContext _ctx;

        public PlatformRepo(AppDbContext ctx) {
            _ctx = ctx;   
        }

        /// <summary>
        /// Inserts a new platform into the Database. Will Throw if null is passed in.
        /// </summary>
        /// <param name="plat"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void CreatePlatform(Platform? plat) {
            if (plat is null) throw new ArgumentNullException(nameof(plat));
            _ctx.Platforms.Add(plat);
        }

        public Platform? GetPlatformById(int id) {
            return _ctx.Platforms.FirstOrDefault(p => p.Id == id);
        }

        public IEnumerable<Platform> GetPlatforms() {
            return _ctx.Platforms.ToList();
        }

        public bool SaveChanges() {
            return _ctx.SaveChanges() > 0;
        }
    }
}
```

For Quick development we fill our Database with some Data if Empty
- `Data/PrepDb.cs`
```csharp
namespace PlatformService.Data
public static class PrepDb{
    public static void PrepPopulation(IApplicationBuilder app) {
        using (var serviceScope = app.ApplicationServices.CreateScope()) {
            SeedData(serviceScope.ServiceProvider.GetService<AppDbContext>());
        }
    }

    public static void SeedData(AppDbContext ctx) {
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
```

## What we add to our Programm.cs
```csharp
using Microsoft.EntityFrameworkCore;
using PlatformService.Data;

var builder = WebApplication.CreateBuilder(args);

// we dependencyinject => If someone asks for IPlatformRepo he will get our PlatformRepo implementation
builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();

// we inject our Database context
builder.Services.AddDbContext<AppDbContext>(opts => {
    opts.UseInMemoryDatabase("InMem");
});

// we inject Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


// ... everyhing else we can just leave untouched


// we manually (for testing/quick-development) inject some fake data into our db
PrepDb.PrepPopulation(app);

app.Run();
```

# Dockerize this service
- https://docs.docker.com/language/dotnet/build-images/

- `Dockerfile`
```Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-envFROM
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-envFROM /app/out .
ENTRYPOINT [ "dotnet", "PlatformService.dll" ]
```

```
// build our docker container
docker build -t vincepr/platformservice .

// run our created container listening on port 8080 mapped to inside the container 80 (so http).
docker run -p 8080:80 -d vincepr/platformservice

// show running containers, then use containerID to kill the container
docker ps
docker stop 4edfbdeac6e9
// if we docker run -p 8080:80 -d vincepr/platformservice we create A NEW container
// we can restart the the same container using the id
docker start 4edfbdeac6e9
// or using the vscode-extension/docker-desktop etc
```
![Alt text](./docker.png)

```
// pushing up to hub.docker.com
docker push vincepr/platformservice
// now the docker repo should show up on your hub.docker.com account:
```
![Alt text](./dockerhub.png)