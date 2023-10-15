using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformService.Dtos
{
    public class PlatformCreateDto
    {
        [Required]
        public required string Name { get; set; }
        [Required]
        public required string Publisher { get; set; }
        [Required]
        public required string Cost { get; set; }
    }
}