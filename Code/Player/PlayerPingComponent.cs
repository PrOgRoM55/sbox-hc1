namespace Facepunch;

/// <summary>
/// A simple component that handles pinging for the player.
/// </summary>
public partial class PlayerPingComponent : Component
{
	/// <summary>
	/// The player
	/// </summary>
	[RequireComponent] PlayerController Player { get; set; }

	/// <summary>
	/// How far can we ping?
	/// </summary>
	[Property] public float PingDistance { get; set; } = 10000f;

	/// <summary>
	/// How long do we exist for?
	/// </summary>
	[Property] public float Lifetime { get; set; } = 15f;

	/// <summary>
	/// Store a reference to the last ping this player placed.
	/// </summary>
	WorldPingComponent WorldPing { get; set; }

	/// <summary>
	/// Sends a ping to people on the server, can only be called by the owner of this player.
	/// This gets networked to people on the same team.
	/// </summary>
	/// <param name="position"></param>
	[Broadcast( NetPermission.OwnerOnly )]
	public void Ping( Vector3 position )
	{
		// Destroy any active pings
		if ( WorldPing.IsValid() )
			WorldPing?.GameObject?.Destroy();

		var pingObject = new GameObject();
		pingObject.Transform.Position = position;
		pingObject.Transform.ClearInterpolation();

		var ping = pingObject.Components.Create<WorldPingComponent>();
		ping.Owner = Player.PlayerState;
		pingObject.Name = $"Ping from {ping.Owner.Network.OwnerConnection.DisplayName}";

		WorldPing = ping;
		// trigger the ping to be destroyed at some point
		ping.Trigger( Lifetime );
	}

	/// <summary>
	/// Can we ping?
	/// </summary>
	/// <returns></returns>
	private bool CanPing()
	{
		var tr = GetTrace();
		if ( !tr.Hit )
			return false;

		return Player.HealthComponent.State == LifeState.Alive;
	}

	/// <summary>
	/// Trace for the ping
	/// </summary>
	/// <returns></returns>
	private SceneTraceResult GetTrace()
	{
		var tr = Scene.Trace.Ray( Player.AimRay, PingDistance )
			.IgnoreGameObjectHierarchy( Player.GameObject.Root )
			.Run();

		return tr;
	}

	protected override void OnUpdate()
	{
		if ( !Player.IsLocallyControlled )
			return;

		// Are we wanting to ping, can we ping?
		if ( Input.Pressed( "Ping" ) && CanPing() )
		{
			var tr = GetTrace();

			// Send a RPC to my teammates
			using ( NetworkUtils.RpcMyTeam() )
			{
				Ping( tr.EndPosition );
			}
		}
	}
}