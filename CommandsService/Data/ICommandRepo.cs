using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandsService.Models;

namespace CommandsService.Data;

public interface ICommandRepo
{
    bool SaveChanges();

    // Platforms
    IEnumerable<Platform> GetAllPlatforms();
    void CreatePlatform(Platform plat);
    bool PlatformExists(int platId);      
    
    // Commands
    IEnumerable<Command> GetCommandsForPlatform(int platId);
    Command GetCommand(int platId, int commandId);
    void CreateCommand(int platId, Command command);
}