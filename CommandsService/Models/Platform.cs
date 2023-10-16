using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommandsService.Models;

public class Platform
{
    public required int Id { get; set; }
    public required int ExternalId { get; set; }
    public required string Name { get; set; }
    public ICollection<Command> Commands { get; set; } = new List<Command>();
}