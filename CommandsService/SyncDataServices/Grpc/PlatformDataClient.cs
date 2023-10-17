using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommandsService.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using PlatformService;

namespace CommandsService.SyncDataServices.Grpc;

public class PlatformDataClient : IPlatformDataClient
{
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;

    public PlatformDataClient(IConfiguration config, IMapper mapper)
    {
        _config = config;
        _mapper = mapper;
    }
    public IEnumerable<Platform> ReturnAllPlatforms()
    {
        Console.WriteLine($"--> Calling gRPC Service {_config["GrpcPlatform"]}");
        var channel = GrpcChannel.ForAddress(_config["GrpcPlatform"]!);
        var client = new GrpcPlatform.GrpcPlatformClient(channel);
        var request = new GetAllRequests(); // even though this is empty we still have to build it and send it over

        try
        {
            var reply = client.GetAllPlatforms(request);
            return _mapper.Map<IEnumerable<Platform>>(reply.Platform);
        }
        catch (Exception e)
        {
            Console.WriteLine($"--> Could not call gRPC Server! {e.Message}");
            return Enumerable.Empty<Platform>();
        }
    }
}