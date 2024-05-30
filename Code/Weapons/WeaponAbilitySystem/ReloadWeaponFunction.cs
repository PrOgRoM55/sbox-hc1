using System.Threading.Tasks;

namespace Facepunch;

public partial class ReloadWeaponFunction : InputActionWeaponFunction
{
	/// <summary>
	/// How long does it take to reload?
	/// </summary>
	[Property] public float ReloadTime { get; set; } = 1.0f;

	/// <summary>
	/// How long does it take to reload while empty?
	/// </summary>
	[Property] public float EmptyReloadTime { get; set; } = 2.0f;


	[Property] public bool SingleReload { get; set; } = false;

	/// <summary>
	/// This is really just the magazine for the weapon. 
	/// </summary>
	[Property] public AmmoContainer AmmoContainer { get; set; }

	bool IsReloading;
	TimeUntil TimeUntilReload;

	protected override void OnEnabled()
	{
		BindTag( "reloading", () => IsReloading );
	}

	protected override void OnFunctionExecute()
	{
		if ( CanReload() )
		{
			StartReload();
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		if ( IsReloading && TimeUntilReload )
		{
			EndReload();
		}
	}

	bool CanReload()
	{
		return !IsReloading && AmmoContainer.IsValid() && !AmmoContainer.IsFull;
	}

	float GetReloadTime()
	{
		if ( !AmmoContainer.HasAmmo ) return EmptyReloadTime;
		return ReloadTime;
	}

	Dictionary<float, SoundEvent> GetReloadSounds()
	{
		if ( !AmmoContainer.HasAmmo ) return EmptyReloadSounds;
		return TimedReloadSounds;
	}

	[Broadcast( NetPermission.OwnerOnly )]
	void StartReload()
	{
		if ( !IsProxy )
		{
			IsReloading = true;
			TimeUntilReload = GetReloadTime();
		}

		// Tags will be better so we can just react to stimuli.
		Weapon.ViewModel?.ModelRenderer.Set( "b_reload", true );

		foreach ( var kv in GetReloadSounds() )
		{
			PlayAsyncSound( kv.Key, kv.Value );
		}
	}

	[Broadcast(NetPermission.OwnerOnly)]
	void EndReload()
	{
		if ( !IsProxy )
		{
			if ( SingleReload )
			{
				AmmoContainer.Ammo++;
				AmmoContainer.Ammo = AmmoContainer.Ammo.Clamp( 0, AmmoContainer.MaxAmmo );

				// Reload more!
				if ( AmmoContainer.Ammo < AmmoContainer.MaxAmmo )
					StartReload();
				else
					IsReloading = false;
			}
			else
			{
				IsReloading = false;
				// Refill the ammo container.
				AmmoContainer.Ammo = AmmoContainer.MaxAmmo;
			}
		}

		// Tags will be better so we can just react to stimuli.
		Weapon.ViewModel?.ModelRenderer.Set( "b_reload", false );
	}

	[Property] public Dictionary<float, SoundEvent> TimedReloadSounds { get; set; } = new();
	[Property] public Dictionary<float, SoundEvent> EmptyReloadSounds { get; set; } = new();

	async void PlayAsyncSound( float delay, SoundEvent snd )
	{
		await GameTask.DelaySeconds( delay );
		GameObject.PlaySound( snd );
	}
}
