# part 6 - multi resource api
In this step we extend the CommandsService to do some actual work. (while using information it gathered from the PlatformService)

|Action|Verb| | Controller|
|---|---|---|---|
|GetAllPlaforms|GET|/api/c/platforms|Platform|
|GetAllCommands ForPlatform|GET|/api/c/platf1orms/{platformId}/commands|Command|
|GetOneCommand ForPlatform|GET|/api/c/platf1orms/{platformId}/commands/{commandId}|Command|
|CreateOneCommand ForPlaform|POST/api/c/platf1orms/{platformId}/commands/|Command|

## Code
- in `Models/` we add our 2 Models
```csharp
public class Command
{
    [Key, Required]
    public int Id { get; set; }
    public required string HowTo { get; set; }
    public required string CommandLine { get; set; }
    public required int PlatformId { get; set; }
    public required Platform Platform { get; set; }
}
```

```csharp
public class Platform
{
    public int Id { get; set; }
    public int ExternalId { get; set; }
    public required string Name { get; set; }
    public ICollection<Command> Commands { get; set; } = new List<Command>();
}
```

- in `Data/` we add our DbContext
```csharp
public class AppDbContext : DbContext
{
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
public class CommandRepo : ICommandRepo
{
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
public interface ICommandRepo
{
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