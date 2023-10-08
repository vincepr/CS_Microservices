# Platform Service
part of the https://github.com/vincepr/CS_Microservices Project
on 1:41:31

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

public class PlatformsProfile : Profile
{
    public PlatformsProfile()
    {
        // <from Source, to Target>

        // Because Names of attributes match 1:1 this is all it needs
        CreateMap<Platform, PlatformReadDto>(); 
        CreateMap<PlatformCreateDto, Platform>();
    }
}
```