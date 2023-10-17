using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


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
        CreateMap<PlatformReadDto, PlatformPublishdDto>();

        // mapping for gRPC:
        CreateMap<Platform, GrpcPlatformModel>()
            // even platformId is camelcase in .proto the generated one gets 'csharped' to PlatformId
            .ForMember(dest => dest.PlatformId, opt => opt.MapFrom(src => src.Id))
            // the other ForMembers would get inferred (because same name) but we do it just to show it more clear
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher));
    }
}