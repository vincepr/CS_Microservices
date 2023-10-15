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
    }
}