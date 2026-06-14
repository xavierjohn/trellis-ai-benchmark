namespace OrderManagement.Api.Application.Auth;

/// <summary>Supplies the current actor for the request scope.</summary>
public interface IActorProvider
{
    Actor GetCurrentActor();
}
