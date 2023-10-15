using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandsService.Models;

namespace CommandsService.Data;

public class CommandRepo : ICommandRepo
{
    private readonly AppDbContext _ctx;

    public CommandRepo(AppDbContext ctx)
    {
        _ctx = ctx;
    }
    public void CreateCommand(int platId, Command command)
    {
        if(command is null) throw new ArgumentNullException(nameof(command));
        command.PlatformId = platId;
        _ctx.Commands.Add(command);
    }

    public void CreatePlatform(Platform plat)
    {
        if(plat is null) throw new ArgumentNullException(nameof(plat));
        _ctx.Platforms.Add(plat);
    }

    public IEnumerable<Platform> GetAllPlatforms()
    {
        return _ctx.Platforms.ToList();
    }

    public Command? GetCommand(int platId, int commandId)
    {
        return _ctx.Commands
            .Where(c => c.PlatformId == platId && c.Id == commandId)
            .FirstOrDefault();
    }

    public IEnumerable<Command> GetCommandsForPlatform(int platId)
    {
        return _ctx.Commands
        .Where(c => c.PlatformId == platId)
        .OrderBy(c => c.Platform.Name);
    }

    public bool PlatformExists(int platId)
    {
        return _ctx.Platforms.Any(p => p.Id == platId);
    }

    public bool SaveChanges()
    {
        return _ctx.SaveChanges() >= 0;
    }
}