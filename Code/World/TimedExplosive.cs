﻿
using Facepunch;
using Sandbox.Utility;
using Sandbox;

public sealed class TimedExplosive : Component, IUse, IMinimapIcon
{
	[Property, Category( "Config" )]
	public float Duration { get; set; } = 45f;

	/// <summary>
	/// How long the defuse will take without a defuse kit.
	/// </summary>
	[Property, Category( "Config" )]
	public float BaseDefuseTime { get; set; } = 10f;

	/// <summary>
	/// How long the defuse will take with a defuse kit.
	/// </summary>
	[Property, Category( "Config" )]
	public float FastDefuseTime { get; set; } = 5f;

	/// <summary>
	/// How long the defuse will take, based on if the defuser has a defuse kit.
	/// </summary>
	public float DefuseTime => DefusingPlayer?.Inventory.HasDefuseKit ?? false
		? FastDefuseTime
		: BaseDefuseTime;

	public float Progress => Math.Clamp( TimeSinceDefuseStart / DefuseTime, 0f, 1f );

	[HostSync] public TimeSince TimeSinceDefuseStart { get; private set; }
	[HostSync] public TimeSince TimeSincePlanted { get; private set; }
	[HostSync]public bool IsDefused { get; private set; }

	public TimeSince TimeSinceLastBeep { get; private set; }

	[Property, Category( "Effects" )]
	public SoundEvent BeepSound { get; set; }

	[Property, Category( "Effects" )]
	public SoundEvent DefuseStartSound { get; set; }

	[Property, Category( "Effects" )]
	public SoundEvent DefuseEndSound { get; set; }

	[Property, Category( "Effects" )]
	public Curve BeepFrequency { get; set; } = new Curve( new Curve.Frame( 0f, 1f ), new Curve.Frame( 1f, 0.25f ) );

	[Property, Category( "Effects" )]
	public GameObject ExplosionPrefab { get; set; }

	[Property, Category( "Effects" )]
	public GameObject BeepEffectPrefab { get; set; }

	[Property, Category( "Effects" )]
	public LineRenderer Wires { get; set; }

	/// <summary>
	/// Bomb site this bomb was planted at.
	/// </summary>
	private BombSite BombSite { get; set; }

	[RequireComponent] public Spottable Spottable { get; private set; }

	public PlayerController DefusingPlayer { get; private set; }

	MinimapIconType IMinimapIcon.IconType => MinimapIconType.PlantedC4;

	Vector3 IMinimapElement.WorldPosition => Transform.Position;

	protected override void OnStart()
	{
		TimeSincePlanted = 0f;
		TimeSinceLastBeep = 0f;
		
		BombSite = Zone.GetAt( Transform.Position )
			.Select( x => x.Components.Get<BombSite>() )
			.FirstOrDefault( x => x is not null );

		if ( !BombSite.IsValid() )
			Log.Warning( $"Bomb site is null!" );
		
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsDefused )
		{
			return;
		}

		BeepEffects();

		if ( !Networking.IsHost ) return;

		if ( DefusingPlayer is not null )
		{
			if ( TimeSinceDefuseStart >= DefuseTime )
			{
				FinishDefusing();
			}
			else if ( !DefusingPlayer.IsValid() || !DefusingPlayer.IsUsing || DefusingPlayer.HealthComponent.State != LifeState.Alive )
			{
				CancelDefusing();
			}
		}

		if ( TimeSincePlanted > Duration )
		{
			Enabled = false;
			Explode();
		}
	}

	private void Explode()
	{
		if ( ExplosionPrefab is null ) return;

		var explosion = ExplosionPrefab.Clone( Transform.Position, Rotation.FromYaw( Random.Shared.NextSingle() * 360f ) );

		explosion.NetworkSpawn();

		foreach ( var listener in Scene.GetAllComponents<IBombDetonatedListener>() )
		{
			listener.OnBombDetonated( GameObject, BombSite );
		}

		if ( BombSite is not null )
		{
			// Bomb site determines damage, so safe radius can be tuned by the mapper

			foreach ( var health in Scene.GetAllComponents<HealthComponent>() )
			{
				var diff = health.Transform.Position - Transform.Position;
				var damage = BombSite.GetExplosionDamage( diff.Length );

				if ( damage <= 0f )
				{
					continue;
				}

				health.TakeDamage( damage, Transform.Position, diff.Normal * damage * 100f, Guid.Empty, Id );
			}
		}

		GameObject.Destroy();
	}

	private void BeepEffects()
	{
		var t = Math.Clamp( TimeSincePlanted / Duration, 0f, 1f );

		if ( TimeSinceLastBeep > BeepFrequency.Evaluate( t ) )
		{
			TimeSinceLastBeep = 0f;

			if ( BeepSound is not null )
			{
				Sound.Play( BeepSound, Transform.Position );
			}

			if ( BeepEffectPrefab.IsValid() )
			{
				BeepEffectPrefab.Clone( Transform.Position + Vector3.Up * 4f );
			}
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void StartDefusing( Guid playerId )
	{
		var player = Scene.Directory.FindComponentByGuid( playerId ) as PlayerController;

		if ( player is null )
		{
			Log.Warning( $"Unknown defuser {playerId}!" );
			return;
		}

		DefusingPlayer = player;
		Wires.Points[1] = DefusingPlayer.CameraGameObject;

		if ( DefuseStartSound is not null )
		{
			Sound.Play( DefuseStartSound, Transform.Position );
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void FinishDefusing()
	{
		if ( Networking.IsHost )
		{
			IsDefused = true;
			DefusingPlayer.IsFrozen = false;

			foreach ( var listener in Scene.GetAllComponents<IBombDefusedListener>() )
			{
				listener.OnBombDefused( DefusingPlayer, GameObject, BombSite );
			}
		}

		Wires.Points[1] = null;
		DefusingPlayer = null;

		if ( DefuseEndSound is not null )
		{
			Sound.Play( DefuseEndSound, Transform.Position );
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void CancelDefusing()
	{
		if ( Networking.IsHost )
		{
			DefusingPlayer.IsFrozen = false;
		}

		Wires.Points[1] = null;
		DefusingPlayer = null;
	}

	public bool CanUse( PlayerController player )
	{
		return !IsDefused && !DefusingPlayer.IsValid() && player.TeamComponent.Team == Team.CounterTerrorist;
	}

	public void OnUse( PlayerController player )
	{
		TimeSinceDefuseStart = 0f;
		StartDefusing( player.Id );

		RadioSounds.Play( player.TeamComponent.Team, "Hidden", "Defusing the bomb" );

		player.IsFrozen = true;
	}

	bool IMinimapElement.IsVisible( PlayerController viewer )
	{
		if ( Spottable.IsSpotted )
			return true;

		return viewer.TeamComponent.Team == Team.Terrorist;
	}
}