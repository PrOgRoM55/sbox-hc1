namespace Facepunch;

public sealed class VehicleSeat : Component
{
	[Property] public Vehicle Vehicle { get; set; }
	[Property] public bool HasInput { get; set; } = true;
	[Property] public List<VehicleExitVolume> ExitVolumes { get; set; }

	[HostSync] public PlayerPawn Player { get; private set; }

	public bool CanEnter( PlayerPawn player )
	{
		return !Player.IsValid();
	}

	[Broadcast( NetPermission.HostOnly )]
	private void RpcEnter( PlayerPawn player )
	{
		Player = player;
		player.CurrentSeat = this;

		Network.AssignOwnership( Player.Network.OwnerConnection );
	}

	public bool Enter( PlayerPawn player )
	{
		if ( !CanEnter( player ) )
		{
			return false;
		}

		using var _ = Rpc.FilterInclude( Connection.Host );

		RpcEnter( player );

		return true;
	}

	public bool CanLeave( PlayerPawn player )
	{
		if ( !Player.IsValid() ) return false;
		if ( Player != player ) return false;

		return true;
	}

	[Broadcast( NetPermission.HostOnly )]
	private void RpcLeave()
	{
		Player.CurrentSeat = null;
		Player = null;

		Network.DropOwnership();
	}
	
	public bool Leave( PlayerPawn player )
	{
		if ( !CanLeave( player ) )
		{
			return false;
		}

		using var _ = Rpc.FilterInclude( Connection.Host );

		RpcLeave();

		return true;
	}

	public Vector3 FindExitLocation()
	{
		// TODO: Multiple volumes (e.g. fallback)
		return ExitVolumes[0].CheckClosestFreeSpace( Transform.Position );
	}

	internal void Eject()
	{
		Leave( Player );
	}
}
