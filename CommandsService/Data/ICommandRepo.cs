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

    /// <summary>
    /// Checks if we already added this ExternalPlatform to our data. If so were synced up. Makes sure we dont duplicate data. 
    /// </summary>
    bool ExternalPlatformExist(int ExternalPlatformId);

    // Commands
    IEnumerable<Command> GetCommandsForPlatform(int platId);
    Command? GetCommand(int platId, int commandId);
    void CreateCommand(int platId, Command command);
}