using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformService.Dtos;

public class PlatformPublishdDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }  
    public required string Event { get; set; }
}