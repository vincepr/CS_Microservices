using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommandsService.Data;
using CommandsService.Dtos;
using CommandsService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommandsService.Controllers;

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