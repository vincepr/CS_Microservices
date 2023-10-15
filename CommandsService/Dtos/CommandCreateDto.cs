using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandsService.Dtos;

public class CommandCreateDto
{
    public required string HowTo { get; set; }
    public required string CommandLine { get; set; }
    // public required int PlatformId { get; set; }  <- this we get internally!
}