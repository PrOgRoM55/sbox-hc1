using Facepunch.UI;
using Sandbox.Events;

namespace Facepunch;

public partial class PlayerController : IGameEventHandler<DamageGivenEvent>, IGameEventHandler<DamageTakenEvent>
{
	/// <summary>
	/// Called when YOU inflict damage on something
	/// </summary>
	void IGameEventHandler<DamageGivenEvent>.OnGameEvent( DamageGivenEvent eventArgs )
	{
		// Did we cause this damage?
		if ( IsViewer )
		{
			Crosshair.Instance?.Trigger( eventArgs.DamageInfo );
		}
	}

	/// <summary>
	/// Called when YOU take damage from something
	/// </summary>
	void IGameEventHandler<DamageTakenEvent>.OnGameEvent( DamageTakenEvent eventArgs )
	{
		var damageInfo = eventArgs.DamageInfo;

		var position = damageInfo.Attacker?.Transform.Position ?? Vector3.Zero;
		var force = damageInfo.Force.IsNearZeroLength ? Random.Shared.VectorInSphere() : damageInfo.Force;

		AnimationHelper.ProceduralHitReaction( damageInfo.Damage / 100f, force );

		if ( !damageInfo.Attacker.IsValid() ) 
			return;

		// Is this the local player?
		if ( IsViewer )
		{
			DamageIndicator.Current?.OnHit( position );
		}

		Body.DamageTakenPosition = position;
		Body.DamageTakenForce = force.Normal * damageInfo.Damage * Game.Random.Float( 5f, 20f );

		// Headshot effects
		if ( damageInfo.Hitbox.HasFlag( HitboxTags.Head ) )
		{
			// Non-local viewer
			if ( !IsViewer )
			{
				var go = damageInfo.HasHelmet ? HeadshotWithHelmetEffect?.Clone( position ) : HeadshotEffect?.Clone( position );
			}

			var headshotSound = damageInfo.HasHelmet ? HeadshotWithHelmetSound : HeadshotSound;
			if ( headshotSound is not null )
			{
				Sound.Play( headshotSound, position );
			}
		}
		else
		{
			if ( BloodEffect.IsValid() )
			{
				BloodEffect?.Clone( new CloneConfig()
				{
					StartEnabled = true,
					Transform = new( position ),
					Name = $"Blood effect from ({GameObject})"
				} );
			}

			if ( BloodImpactSound is not null )
			{
				var snd = Sound.Play( BloodImpactSound, position );
				snd.ListenLocal = IsViewer;
			}
		}
	}
}
