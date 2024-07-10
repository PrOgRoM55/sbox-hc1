using Sandbox.Diagnostics;
using Sandbox.Events;

namespace Facepunch;

public record OnPlayerRagdolledEvent : IGameEvent
{
	public float DestroyTime { get; set; } = 0f;
}

public partial class PlayerPawn
{
	/// <summary>
	/// The player's health component
	/// </summary>
	[RequireComponent] public ArmorComponent ArmorComponent { get; private set; }

	/// <summary>
	/// The player's inventory, items, etc.
	/// </summary>
	[RequireComponent] public PlayerInventory Inventory { get; private set; }
	
	/// <summary>
	/// How long since the player last respawned?
	/// </summary>
	[HostSync] public TimeSince TimeSinceLastRespawn { get; private set; }

	public override void OnKill( DamageInfo damageInfo )
	{
		if ( Networking.IsHost )
		{
			ArmorComponent.HasHelmet = false;
			ArmorComponent.Armor = 0f;

			PlayerState.RespawnState = RespawnState.Requested;

			Inventory.Clear();
			CreateRagdoll();
		}

		PlayerBoxCollider.Enabled = false;

		if ( IsProxy )
			return;

		PlayerState.OnKill( damageInfo );

		Holster();

		_previousVelocity = Vector3.Zero;
		CameraController.Mode = CameraMode.ThirdPerson;
	}

	public override void OnRespawn()
	{
		Assert.True( Networking.IsHost );

		OnHostRespawn();
		OnClientRespawn();
	}

	private void OnHostRespawn()
	{
		Assert.True( Networking.IsHost );

		_previousVelocity = Vector3.Zero;

		if ( Body is not null )
		{
			Body.DamageTakenForce = Vector3.Zero;
		}

		SpawnPointTags.Clear();

		// :S
		if ( GameMode.Instance.Get<ISpawnAssigner>() is { } spawnAssigner )
		{
			var s = spawnAssigner.GetSpawnPoint( PlayerState );

			foreach ( var tag in s.Tags )
			{
				SpawnPointTags.Add( tag );
			}

			Teleport( s.Transform );
		}

		HealthComponent.Health = HealthComponent.MaxHealth;

		ArmorComponent.HasHelmet = false;
		ArmorComponent.Armor = 0f;

		EyeAngles = Transform.Rotation.Angles();

		TimeSinceLastRespawn = 0f;

		ResetBody();
		Scene.Dispatch( new PlayerSpawnedEvent( this ) );
	}

	[Authority]
	private void OnClientRespawn()
	{
		if ( PlayerState.IsBot )
			return;

		Possess();
	}

	public void Teleport( Transform transform )
	{
		Teleport( transform.Position, transform.Rotation );
	}

	[Authority]
	public void Teleport( Vector3 position, Rotation rotation )
	{
		Transform.World = new( position, rotation );
		Transform.ClearInterpolation();
		EyeAngles = rotation.Angles();

		if ( CharacterController.IsValid() )
			CharacterController.Velocity = Vector3.Zero;
	}

	[Broadcast( NetPermission.HostOnly )]
	private void CreateRagdoll()
	{
		if ( !Body.IsValid() )
			return;

		Body.SetRagdoll( true );
		Body.GameObject.SetParent( null, true );
		Body.GameObject.Name = $"Ragdoll ({DisplayName})";

		var ev = new OnPlayerRagdolledEvent();
		Scene.Dispatch( ev );

		if ( ev.DestroyTime > 0f )
		{
			var comp = Body.Components.Create<TimedDestroyComponent>();
			comp.Time = ev.DestroyTime;
		}
		else
		{
			Body.Components.Create<DestroyBetweenRounds>();
		}

		Body = null;
	}

	private void ResetBody()
	{
		if ( Body is not null )
		{
			Body.DamageTakenForce = Vector3.Zero;
		}

		PlayerBoxCollider.Enabled = true;

		Outfitter.OnResetState( this );

		GameObject.Tags.Set( "invis", false );
	}
}
