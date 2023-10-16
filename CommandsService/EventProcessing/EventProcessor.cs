using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using CommandsService.Data;
using CommandsService.Dtos;
using CommandsService.Models;

namespace CommandsService.EventProcessing;

public class EventProcessor : IEventProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;

    public EventProcessor(
        IServiceScopeFactory scopeFactory,
        IMapper mapper
    )
    {
        _scopeFactory = scopeFactory;
        _mapper = mapper;
    }
    public void ProcessEvent(string message)
    {
        var eventType = DetermineEvent(message);
        switch (eventType)
        {
            case EventType.PlatformPublished:
                //TODO
                break;
            default:
                break;
        }
    }

    private EventType DetermineEvent(string notificationMessage)
    {
        Console.WriteLine("--> Determining event");
        var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

        if (eventType is null)
        {
            Console.WriteLine("--> Serializing event-type wrent wrong. Is Null");
            return EventType.Undetermined;
        }

        switch (eventType.Event)
        {
            case "":
                Console.WriteLine("--> New_Platform_Published event-type detected.");
                return EventType.PlatformPublished;
            default:
                Console.WriteLine("--> Could not determine event-type.");
                return EventType.Undetermined;
        }
    }

    // TODO use AddPlatform
    private void AddPlatform(string platformPublishedMessage)
    {
        // use the scopeFactory to get access to our repository
        // this is neccessary because of the different lifetimes of our repo vs our Singleton-EventProcessing
        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ICommandRepo>();
            var platformPublishedDto = JsonSerializer.Deserialize<PlatformPublishedDto>(platformPublishedMessage);

            try
            {
                var plat = _mapper.Map<Platform>(platformPublishedDto);
                if (!repo.ExternalPlatformExist(plat.ExternalId))
                {
                    repo.CreatePlatform(plat);
                    repo.SaveChanges();
                }
                else
                {
                    Console.WriteLine(" --> Platform already exists in local db...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"--> Could not add Platform do DB; {e.Message}");
            }
        }
    }
}
enum EventType
{
    PlatformPublished,  // <= "New_Platform_Published" as Event string
    Undetermined        // <= any other Event
}