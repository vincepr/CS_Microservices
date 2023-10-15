using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PlatformService.Models
{
    public class Platform
    {
        [Key, Required] // because of the name EF would interpret this as key anyway, but never hurts to be explicit
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        [Required]
        public required string Publisher { get; set; }
        [Required]
        public required string Cost { get; set; }
    }
}