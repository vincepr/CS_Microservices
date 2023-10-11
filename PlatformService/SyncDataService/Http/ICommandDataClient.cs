using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlatformService.Dtos;

namespace PlatformService.SyncDataService.Http
{
    public interface ICommandDataClient
    {
        // is just used to test the connection inside the Kubernetes Cluster
        // - sends over PlatformData when a new Platform is created at this Service
        // - will later get properly implemented with Event-Bus/grpc
        Task SendPlatformTocommand(PlatformReadDto newPlat);
    }
}