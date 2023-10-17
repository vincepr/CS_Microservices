using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Grpc.Core;
using PlatformService.Data;

namespace PlatformService.SyncDataService.Http.Grpc;

public class GrpcPlatformService : GrpcPlatform.GrpcPlatformBase
{
    private readonly IPlatformRepo _repository;
    private readonly IMapper _mapper;

    public GrpcPlatformService(IPlatformRepo repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public override Task<PlatformResponse> GetAllPlatforms(GetAllRequests request, ServerCallContext context)
    {
        var response = new PlatformResponse();
        var platforms = _repository.GetPlatforms();
        foreach (var p in platforms)
        {
            // map from our.Platform -> grpc.Platfrom and add those to grpc.Response
            response.Platform.Add(_mapper.Map<GrpcPlatformModel>(p));
        }
        return Task.FromResult(response);
    }
}