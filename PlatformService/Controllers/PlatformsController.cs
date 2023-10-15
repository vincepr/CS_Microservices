using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataService.Http;

namespace PlatformService.Controllers;

[Route("api/[controller]")]     // translates to ".../api/Platforms/"
[ApiController] // implements some default behaviours we want from our Controller
public class PlatformsController: ControllerBase
{
    private readonly IPlatformRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICommandDataClient _commandDataClient;
    private readonly IMessageBusClient _messageBusClient;

    public PlatformsController(
        IPlatformRepo repository, 
        IMapper mapper, 
        ICommandDataClient commandDataClient,
        IMessageBusClient messageBusClient)
    {
        _repository = repository;
        _mapper = mapper;
        _commandDataClient = commandDataClient;
        _messageBusClient = messageBusClient;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
        var platformItems = _repository.GetPlatforms();
        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
        var platform = _repository.GetPlatformById(id);
        if (platform is null) return NotFound();
        return Ok(_mapper.Map<PlatformReadDto>(platform));
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto dto)
    {
        var newPlatform = _mapper.Map<Platform>(dto);
        _repository.CreatePlatform(newPlatform);
        if (_repository.SaveChanges() == false) return StatusCode(500);
         
        var platformReadDto = _mapper.Map<PlatformReadDto>(newPlatform);

        // We send a Synchronous Message to notify about creation
        try {
            await _commandDataClient.SendPlatformTocommand(platformReadDto);
        } catch (Exception e) {
            Console.WriteLine("$--> Could not send synchronously! " + e.Message);
        }

        // We send a Async Message via our Bus to notify about creation
        try {
            var platformPublishedDto = _mapper.Map<PlatformPublishdDto>(platformReadDto);
            platformPublishedDto.Event = "New_Platform_Published";
            _messageBusClient.PublishNewPlatform(platformPublishedDto);

        } catch (Exception e) {
            Console.WriteLine("$--> Could not send Asynchronously! " + e.Message);
        }

        return CreatedAtRoute(
            nameof(GetPlatformById),            // provides a link to the /api/get/{newId}
            new { Id = platformReadDto.Id },    // the id of our newly created obj
            platformReadDto);                   // and we also return the dtoObject directly
    }
}