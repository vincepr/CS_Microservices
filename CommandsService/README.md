# part4/1 - Commands Service

## Setup
```
dotnet new webapi -n CommandsService

dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

## in CommandsService project(this project)
an Endpoint the other Service can post to, to notify this service when he created a new platform. (syncrhous tight coupling)
```csharp
[HttpPost]
public ActionResult TestInboundConnection() {
    Console.WriteLine("--> Inbound POST # Command Service");
    return Ok("Inbound test of from Platforms Controller");
}
```
### in PlatformService project(the other project)
We modify the other endpoint to pass down an notification everytime it creates a new platform.
```csharp
[HttpPost]
public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto dto)
{
    var newPlatform = _mapper.Map<Platform>(dto);
    _repository.CreatePlatform(newPlatform);
    if (_repository.SaveChanges() == false) return StatusCode(500);
        
    var platformReadDto = _mapper.Map<PlatformReadDto>(newPlatform);

    try {
        await _commandDataClient.SendPlatformTocommand(platformReadDto);
    } catch (Exception e) {
        Console.WriteLine("$--> Could not send synchronousl. " + e.Message);
    }

    return CreatedAtRoute(
        nameof(GetPlatformById),            // provides a link to the /api/get/{newId}
        new { Id = platformReadDto.Id },    // the id of our newly created obj
        platformReadDto);                   // and we also return the dtoObject directly
}
```
- we also change launchSetting.json to use different ports for each Service
- and statically let our PlatformService know where to send data to: `appsettings.Development.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "CommandService": "http://localhost:6000/api/c/platforms/"
}
```
- sending a post request happens in `SyncDataService/Http/HttpCommandDataClient.cs`, that we dependencyinject with an interface to use in our Endpoint above.
```csharp
namespace PlatformService.SyncDataService.Http {
    public class HttpCommandDataClient : ICommandDataClient {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // IConfiguration can be injected basically everywhere.
        // - here we use it to read out from our appsettings.json
        public HttpCommandDataClient(HttpClient httpClient, IConfiguration configuration) {
            _httpClient = httpClient;
            _configuration = configuration;

        }

        public async Task SendPlatformTocommand(PlatformReadDto newPlat) {
            var httpBody = new StringContent(
                JsonSerializer.Serialize(newPlat),
                Encoding.UTF8,
                "application/json");
            var resp = await _httpClient.PostAsync($"{_configuration["CommandService"]}", httpBody);
            if (resp.IsSuccessStatusCode) 
                Console.WriteLine("--> Sync POST to CommandService was OK.");
            else
                Console.WriteLine("--> Sync POST to CommandService was NOT ok!");
        }
    }
}
```

## getting Commands Services ready for Kubernetes
- Create the Dockerfile
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
ENTRYPOINT [ "dotnet", "CommandsService.dll" ]
```
- and push it to Dockerhub
```
docker build -t vincepr/commandservice .
docker push vincepr/commandservice
```
- next we add `/K8S/commands-depl.yaml`
    - basically the same deployment as for the PlatformService but with changed names
    - next we add the **ClusterIpService** to both delpoyments (everything above the `---`)
        - these ClusterIPServices enable direct communication inside our Kubernetes Cluster
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: commands-depl
spec:
  # replicas are basically horizontal scaling (ex multiple api containers that run at the same time etc...)
  replicas: 1
  # selector and template are defining the template were creating
  selector:
    matchLabels:
      app: commandservice
  template:
    metadata:
      labels:
        app: commandservice
    spec:
      containers:
        ## we use our previously created docker containers here
        - name: commandservice
          image: vincepr/commandservice:latest
---
# we could put this in a separate file, --- separates this as a 'new' one
# We add a new ClusterIpService to our Deployment
apiVersion: v1
kind: Service
metadata:
  name: commands-clusterip-srv
spec:
  type: ClusterIP
  selector:
    app: commandservice
  ports:
    - name: commandservice
      protocol: TCP
      port: 80
      targetPort: 80
```

- since PlatformService needs to know the exact location and port of the endpoint to send the POST request to CommandsService to, we have to add a `appsettings.Production.json`
```json
{
    "CommandService": "http://commands-clusterip-srv:80/api/c/platforms/"
}
```
- now we have to rebuild the platformservice docker container with the new appsettings included:
```
docker build -t vincepr/platformservice ./PlatformService
docker push vincepr/platformservice
```

**NOTE**: it would be better to extract those settings outside of the docker image itself and work with for examples env paramaters. But this is not scope of this tutorial.

## starting Kubernetes up
- first we check previous state:
```
kubectl get deployments
# NAME             READY   UP-TO-DATE   AVAILABLE   AGE
# platforms-depl   1/1     1            1           3d15h

kubectl get pods
# NAME                              READY   STATUS    RESTARTS       AGE
# platforms-depl-85677fb59d-7bgf7   1/1     Running   2 (114m ago)   3d15h

kubectl get services
# NAME                    TYPE        CLUSTER-IP     EXTERNAL-IP   PORT(S)        AGE
# kubernetes              ClusterIP   10.96.0.1      <none>        443/TCP        4d15h
# platformnpservice-srv   NodePort    10.103.51.73   <none>        80:30085/TCP   3d15h
```

- we apply our changed file:
    - **NOTICE** how the new platformsclusterip-srv has started up.
    - **BUT** what kubernetes did not do, was go get the latest dockerfile from dockerhub (the one including our added appsettings.Development.json)
```
kubectl apply -f ./K8S/platforms-depl.yaml
# deployment.apps/platforms-depl unchanged
# service/platforms-clusterip-srv created

kubectl get services
# NAME                      TYPE        CLUSTER-IP      EXTERNAL-IP   PORT(S)        AGE
# kubernetes                ClusterIP   10.96.0.1       <none>        443/TCP        4d15h        
# platformnpservice-srv     NodePort    10.103.51.73    <none>        80:30085/TCP   3d15h        
# platforms-clusterip-srv   ClusterIP   10.97.239.139   <none>        80/TCP         46s
```

-  so we force kubernetes to update to the latest version:
```
kubectl rollout restart deployment platforms-depl
```
When we check the logs for that freshly started Container:
- we can see that it got the right endpoint ` http://commands-clusterip-srv:80/]`
```
2023-10-14 13:36:01 info: Microsoft.EntityFrameworkCore.Update[30100]
2023-10-14 13:36:01       Saved 3 entities to in-memory store.
2023-10-14 13:36:01 ---> Seeding Data with some made up Data
2023-10-14 13:36:01 --> config[CommandService] endpoint: http://platforms-clusterip-srv:80/api/c/platforms/
```

- so now we finally add our 2nd service to our Cluster
    - now we have 2 Services with one ClusterIp each running
```
kubectl apply -f ./K8S/commands-depl.yaml

NAME                      TYPE        CLUSTER-IP      EXTERNAL-IP   PORT(S)        AGE
# commands-clusterip-srv    ClusterIP   10.105.102.58   <none>        80/TCP         18s
# kubernetes                ClusterIP   10.96.0.1       <none>        443/TCP        4d15h        
# platformnpservice-srv     NodePort    10.103.51.73    <none>        80:30085/TCP   3d15h        
# platforms-clusterip-srv   ClusterIP   10.97.239.139   <none>        80/TCP         10m
```

- now we POST to our exposed enpoing `http://localhost:30085/api/platforms/`
- Our platformervice Log reveals it successfully made it's postrequest
```
2023-10-14 15:14:08 info: Microsoft.EntityFrameworkCore.Update[30100]
2023-10-14 15:14:08       Saved 1 entities to in-memory store.
2023-10-14 15:14:08 info: System.Net.Http.HttpClient.ICommandDataClient.LogicalHandler[100]
2023-10-14 15:14:08       Start processing HTTP request POST http://commands-clusterip-srv/api/c/platforms/
2023-10-14 15:14:08 info: System.Net.Http.HttpClient.ICommandDataClient.ClientHandler[100]
2023-10-14 15:14:08       Sending HTTP request POST http://commands-clusterip-srv/api/c/platforms/
2023-10-14 15:14:08 info: System.Net.Http.HttpClient.ICommandDataClient.ClientHandler[101]
2023-10-14 15:14:08       Received HTTP response headers after 106.5811ms - 200
2023-10-14 15:14:08 info: System.Net.Http.HttpClient.ICommandDataClient.LogicalHandler[101]
2023-10-14 15:14:08       End processing HTTP request after 113.9815ms - 200
2023-10-14 15:14:08 --> Sync POST to CommandService was OK.
```
- And our commandservice recieved the inbound request:
```
2023-10-14 15:14:08 warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
2023-10-14 15:14:08       Failed to determine the https port for redirect.
2023-10-14 15:14:08 --> Inbound POST # Command Service
```

















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