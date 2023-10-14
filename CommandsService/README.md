# part4 - Commands Service

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