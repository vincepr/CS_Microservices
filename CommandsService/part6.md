# part 6 - multi resource api
In this step we extend the CommandsService to do some actual work. (while using information it gathered from the PlatformService)

|Action|Verb| |Controller|
|---|---|---|---|
|GetAllPlaforms|GET|/api/c/platforms|Platform|
|GetAllCommands ForPlatform|GET|/api/c/platf1orms/{platformId}/commands|Command|
|GetOneCommand ForPlatform|GET|/api/c/platf1orms/{platformId}/commands/{commandId}|Command|
|CreateOneCommand ForPlaform|POST/api/c/platf1orms/{platformId}/commands/|Command|

## Code
- in `Models/` we add our 2 Models
```csharp
public class Command {
    [Key, Required]
    public int Id { get; set; }
    public required string HowTo { get; set; }
    public required string CommandLine { get; set; }
    public required int PlatformId { get; set; }
    public required Platform Platform { get; set; }
}
```

```csharp
public class Platform {
    public int Id { get; set; }
    public int ExternalId { get; set; }
    public required string Name { get; set; }
    public ICollection<Command> Commands { get; set; } = new List<Command>();
}
```

- in `Data/` we add our DbContext
```csharp
public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts){}
    public DbSet<Platform> Platforms { get; set; }
    public DbSet<Command> Commands { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // usually EF does a decent job infering relationships between Models
        // - but we are explicit here just to make sure (and show how to)
        // - were basically declaring the 1:n relationship between the tables etc...
        modelBuilder
            .Entity<Platform>()
            .HasMany(p => p.Commands)
            .WithOne(p => p.Platform!)
            .HasForeignKey(p => p.PlatformId);
        
        modelBuilder
            .Entity<Command>()
            .HasOne(p => p.Platform)
            .WithMany(p => p.Commands)
            .HasForeignKey(p => p.PlatformId);
    }
}
```
- in `Data/` we add our Repository Pattern
```csharp
public class CommandRepo : ICommandRepo {
    private readonly AppDbContext _ctx;

    public CommandRepo(AppDbContext ctx) {
        _ctx = ctx;
    }
    public void CreateCommand(int platId, Command command) {
        if(command is null) throw new ArgumentNullException(nameof(command));
        command.PlatformId = platId;
        _ctx.Commands.Add(command);
    }

    public void CreatePlatform(Platform plat) {
        if(plat is null) throw new ArgumentNullException(nameof(plat));
        _ctx.Platforms.Add(plat);
    }

    public IEnumerable<Platform> GetAllPlatforms() {
        return _ctx.Platforms.ToList();
    }

    public Command GetCommand(int platId, int commandId) {
        return _ctx.Commands.Where(c => c.PlatformId == platId && c.Id == commandId).FirstOrDefault();
    }

    public IEnumerable<Command> GetCommandsForPlatform(int platId) {
        return _ctx.Commands
        .Where(c => c.PlatformId == platId)
        .OrderBy(c => c.Platform.Name);
    }

    public bool PlatformExists(int platId) {
        return _ctx.Platforms.Any(p => p.Id == platId);
    }

    public bool SaveChanges() {
        return _ctx.SaveChanges() >= 0;
    }
}
```

```csharp
public interface ICommandRepo {
    bool SaveChanges();

    // Platforms
    IEnumerable<Platform> GetAllPlatforms();
    void CreatePlatform(Platform plat);
    bool PlatformExists(int platId);      
    
    // Commands
    IEnumerable<Command> GetCommandsForPlatform(int platId);
    Command GetCommand(int platId, int commandId);
    void CreateCommand(int platId, Command command);
}
```
- we inject those to our `Programm.cs`:
```csharp
// we dependenyc inject:
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseInMemoryDatabase("InMem") );
builder.Services.AddScoped<ICommandRepo, CommandRepo>();
```

- we add Automapper Mappings
```csharp
public class CommandsProfile : AutoMapper.Profile {
    public CommandsProfile() {
        // <Source> -> <Target>
        CreateMap<Platform, PlatformReadDto>();
        CreateMap<CommandCreateDto, Command>();
        CreateMap<Command, CommandReadDto>();
    }
}
```


- we add Dtos for all needed uscases(3 atm)
```csharp
public class CommandCreateDto {
    public required string HowTo { get; set; }
    public required string CommandLine { get; set; }
    // public required int PlatformId { get; set; }  <- this we get internally!
}
```

### And we create the Controllers
- `Controllers/CommandsController`
```csharp
[ApiController]
[Route("api/c/platforms/{platformId}/[controller]")]
public class CommandsController : ControllerBase
{
    private readonly ICommandRepo _repository;
    private readonly IMapper _mapper;

    public CommandsController(ICommandRepo repo, IMapper mapper)
    {
       _repository = repo;
       _mapper = mapper; 
    }

    [HttpGet]
    public ActionResult<IEnumerable<CommandReadDto>> GetAllCommandsByPlatformId(int platformId) {
        Console.WriteLine($"--> Hit GetAllCommandsByPlatformId with platformId={platformId}");
        if (!_repository.PlatformExists(platformId)) return NotFound();
        var commandItems = _repository.GetCommandsForPlatform(platformId);
        return Ok(_mapper.Map<IEnumerable<CommandReadDto>>(commandItems));
    }

    [HttpGet("{commandId}", Name = "GetCommandForPlatform")] // again we give this a Name to be able to reference it later. CreatingNew -> pointing to new createdID@this
    public ActionResult<CommandReadDto> GetCommandForPlatform(int platformId, int commandId) {
        Console.WriteLine($"--> Hit GetCommandForPlatform with platformId={platformId} commandId={commandId}");
        if (!_repository.PlatformExists(platformId)) return NotFound();
        var command = _repository.GetCommand(platformId, commandId);
        if (command is null) return NotFound();
        return Ok(_mapper.Map<CommandReadDto>(command));
    }

    [HttpPost]
    public ActionResult<CommandReadDto> CreateCommandForPlatform(int platformId, CommandCreateDto commandDto) {
        Console.WriteLine($"--> Hit CreateCommandForPlatform with platformId={platformId}");
        if (!_repository.PlatformExists(platformId)) return NotFound();
        var command = _mapper.Map<Command>(commandDto);
        _repository.CreateCommand(platformId, command);
        _repository.SaveChanges();
        var responseDto = _mapper.Map<CommandReadDto>(command);
        return CreatedAtRoute(nameof(GetCommandForPlatform),
            new {platformId=platformId, commandId=responseDto.Id}, responseDto);
    }
}
```
- and we extend the already existing: `Constrollers/PlatformsController`
```csharp
namespace CommandsService.Controllers
{
    [Route("api/c/[controller]")]   // the c is just so we can differentiate our two services for now
    [ApiController]
    public class PlatformsController : ControllerBase
    {
        private readonly ICommandRepo _repository;
        private readonly IMapper _mapper;

        public PlatformsController(ICommandRepo repo, IMapper mapper)
        {
           _repository = repo;
           _mapper = mapper; 
        }

        [HttpPost]
        public ActionResult TestInboundConnection() {
            Console.WriteLine("--> Inbound POST # Command Service");
            return Ok("Inbound test of from Platforms Controller");
        }

        [HttpGet]
        public ActionResult<IEnumerable<PlatformReadDto>> GetAllPlatforms() {
            Console.WriteLine("--> Platforms-data from CommandsService was requested");
            var platformItems = _repository.GetAllPlatforms();
            return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
        }
    }
}
```