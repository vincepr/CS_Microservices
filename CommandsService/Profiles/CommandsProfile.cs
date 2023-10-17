using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandsService.Dtos;
using CommandsService.Models;
using PlatformService;

namespace CommandsService.Profiles;

public class CommandsProfile : AutoMapper.Profile
{
    public CommandsProfile() {
        // <Source> -> <Target>
        CreateMap<Platform, PlatformReadDto>();
        CreateMap<CommandCreateDto, Command>();
        CreateMap<Command, CommandReadDto>();
        CreateMap<PlatformPublishedDto, Platform>()
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.Id));
            // Basically we want to take PlatformPublishedDto.Id and map it to our Platform.ExternalID
        
        // gRPC-Mappings:
        CreateMap<GrpcPlatformModel, Platform>()
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.PlatformId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            // we explicitly tell that we want noting to map to our.Platform.Commands
            .ForMember(dest => dest.Commands, opt => opt.Ignore());
    }
}