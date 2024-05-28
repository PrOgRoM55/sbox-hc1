namespace Facepunch;

[Icon( "track_changes" )]
public partial class MeleeFunction : InputActionWeaponFunction
{
	[Property, Category( "Config" )] public float BaseDamage { get; set; } = 25.0f;
	[Property, Category( "Config" )] public float FireRate { get; set; } = 0.2f;
	[Property, Category( "Config" )] public float MaxRange { get; set; } = 1024000;
	[Property, Category( "Config" )] public float Size { get; set; } = 1.0f;

	[Property, Group( "Sounds" )] public SoundEvent SwingSound { get; set; }

	public TimeSince TimeSinceSwing { get; private set; }

	/// <summary>
	/// Fetches the desired model renderer that we'll focus effects on like trail effects, muzzle flashes, etc.
	/// </summary>
	protected SkinnedModelRenderer EffectsRenderer 
	{
		get
		{
			if ( IsProxy || !Weapon.ViewModel.IsValid() )
			{
				return Weapon.ModelRenderer;
			}

			return Weapon.ViewModel.ModelRenderer;
		}
	}

	/// <summary>
	/// Do shoot effects
	/// </summary>
	protected void DoEffects()
	{
		if ( SwingSound is not null )
		{
			if ( Sound.Play( SwingSound, Weapon.Transform.Position ) is SoundHandle snd )
			{
				snd.ListenLocal = !IsProxy;
			}
		}

		// Third person
		Weapon.PlayerController.BodyRenderer.Set( "b_attack", true );

		// First person
		Weapon.ViewModel?.ModelRenderer.Set( "b_attack", true );
	}

	/// <summary>
	/// Gets a surface from a trace.
	/// </summary>
	/// <param name="tr"></param>
	/// <returns></returns>
	private Surface GetSurfaceFromTrace( SceneTraceResult tr )
	{
		return tr.Surface;	
	}

	private void CreateImpactEffects( GameObject hitObject, Surface surface, Vector3 pos, Vector3 normal )
	{
		var decalPath = Game.Random.FromList( surface.ImpactEffects.BulletDecal, "decals/bullethole.decal" );
		if ( ResourceLibrary.TryGet<DecalDefinition>( decalPath, out var decalResource ) )
		{
			var decal = Game.Random.FromList( decalResource.Decals );

			var gameObject = Scene.CreateObject();
			gameObject.Transform.Position = pos;
			gameObject.Transform.Rotation = Rotation.LookAt( -normal );

			// Random rotation
			gameObject.Transform.Rotation *= Rotation.FromAxis( Vector3.Forward, decal.Rotation.GetValue() );

			var decalRenderer = gameObject.Components.Create<DecalRenderer>();
			decalRenderer.Material = decal.Material;
			decalRenderer.Size = new( decal.Width.GetValue(), decal.Height.GetValue(), decal.Depth.GetValue() );

			// Creates a destruction component to destroy the gameobject after a while
			gameObject.DestroyAsync( 3f );
		}

		if ( !string.IsNullOrEmpty( surface.Sounds.Bullet ) )
		{
			hitObject.PlaySound( surface.Sounds.Bullet );
		}
	}

	public void Swing()
	{
		TimeSinceSwing = 0;

		int count = 0;

		foreach ( var tr in GetTrace() )
		{
			if ( !tr.Hit )
			{
				DoEffects();
				return;
			}

			DoEffects();
			CreateImpactEffects( tr.GameObject, GetSurfaceFromTrace( tr ), tr.EndPosition, tr.Normal );

			// Inflict damage on whatever we find.
			var damageInfo = DamageInfo.Sharp( BaseDamage, Weapon.PlayerController.GameObject, Weapon.GameObject );
			tr.GameObject.TakeDamage( ref damageInfo );
			count++;
		}
	}

	protected virtual Ray WeaponRay => Weapon.PlayerController.AimRay;

	/// <summary>
	/// Runs a trace with all the data we have supplied it, and returns the result
	/// </summary>
	/// <returns></returns>
	protected virtual IEnumerable<SceneTraceResult> GetTrace()
	{
		var start = WeaponRay.Position;
		var end = WeaponRay.Position + WeaponRay.Forward * MaxRange;

        yield return Scene.Trace.Ray( start, end )
			.UseHitboxes()
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.WithoutTags( "trigger" )
			.Size( Size )
			.Run();
	}

	/// <summary>
	/// Can we shoot this gun right now?
	/// </summary>
	/// <returns></returns>
	public bool CanSwing()
	{
		// Player
		if ( Weapon.PlayerController.HasTag( "sprint" )  )
			return false;

        // Delay checks
        if ( TimeSinceSwing < FireRate )
		{
			return false;
		}

        return true;
	}

    protected override void OnFunctionExecute()
	{
		if ( CanSwing() )
		{
			Swing();
		}
	}
}
