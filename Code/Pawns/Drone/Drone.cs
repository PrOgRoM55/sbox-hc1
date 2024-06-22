namespace Facepunch;

public partial class Drone : Pawn, IRespawnable, ICustomMinimapIcon
{
	// TODO: Make a CameraController component that can be re-used between this, and other pawns.
	[Property, Group( "Components" )] public DroneCamera CameraController { get; set; }
	[Property, Group( "Components" )] public Rigidbody Rigidbody { get; set; }
	[Property, Group( "Components" )] public ModelRenderer Model { get; set; }

	[Property, Group( "Visuals" )] public List<GameObject> Turbines { get; set; } = new();

	[Property, Group( "Config" )] public float AltitudeAcceleration { get; set; } = 1000;
	[Property, Group( "Config" )] public float MovementAcceleration { get; set; } = 2500;
	[Property, Group( "Config" )] public float YawSpeed { get; set; } = 3000;
	[Property, Group( "Config" )] public float UprightSpeed { get; set; } = 5000;
	[Property, Group( "Config" )] public float UprightDot { get; set; } = 0.5f;
	[Property, Group( "Config" )] public float LeanWeight { get; set; } = 0.5f;
	[Property, Group( "Config" )] public float LeanMaxVelocity { get; set; } = 1000;

	[RequireComponent] public DroneSounds DroneSounds { get; set; }
	[RequireComponent] public TeamComponent TeamComponent { get; set; }

	/// <summary>
	/// What to spawn when we explode?
	/// </summary>
	[Property] public GameObject Explosion { get; set; }

	public override Angles EyeAngles => Transform.Rotation.Angles();

	/// <summary>
	/// What are we called?
	/// </summary>
	public override string DisplayName => Network.OwnerConnection.DisplayName + "'s drone";

	// should just set the bone positions 
	private readonly Vector3[] turbinePositions = new Vector3[]
	{
		new Vector3( -35.37f, 35.37f, 10.0f ),
		new Vector3( 35.37f, 35.37f, 10.0f ),
		new Vector3( 35.37f, -35.37f, 10.0f ),
		new Vector3( -35.37f, -35.37f, 10.0f )
	};

	protected override void OnStart()
	{
		// TODO: don't do it like this
		if ( !IsProxy )
		{
			Possess();
			HealthComponent.Health = 100f;
		}
	}

	private DroneInputState currentInput;
	private float spinAngle;

	protected override void OnFixedUpdate()
	{
		ApplyForces();
	}

	void Depossess()
	{
		if ( IsLocallyControlled )
		{
			DePossess();
		}
	}

	public void Kill()
	{
		Depossess();

		Explosion?.Clone( Transform.Position );
        Tags.Set( "invis", true );
	}

	protected override void OnDestroy()
	{
		// Just in case
		Depossess();
	}

	protected override void OnUpdate()
	{
		if ( IsLocallyControlled )
		{
			currentInput.Reset();
			currentInput.movement = Input.AnalogMove.WithZ( 0f ).Normal;
			currentInput.throttle = (Input.Down( "Jump" ) ? 1 : 0) + (Input.Down( "Duck" ) ? -1 : 0);
			currentInput.yaw = -Input.AnalogLook.yaw;
		}

        // nasty cleanup, TODO: remove this
        if ( !IsProxy )
        {
            if ( HealthComponent.TimeSinceLifeStateChanged > 1f && HealthComponent.State != LifeState.Alive )
            {
                GameObject?.Destroy();
            }
        }

        spinAngle += 5000.0f * Time.Delta;
        spinAngle %= 360.0f;

        for ( int i = 0; i < Turbines.Count; i++ )
        {
	        var turbine = Turbines[i];
	        var pos = turbinePositions[i];
	        turbine.Transform.Rotation = Rotation.From( new Angles( 0, spinAngle, 0 ) );
	        turbine.Transform.LocalPosition = pos;
        }
	}

	protected void ApplyForces()
	{
		if ( !IsLocallyControlled )
			return;

		if ( !Rigidbody.IsValid() )
			return;

		var body = Rigidbody;
		var transform = Transform.World;

		body.LinearDamping = 4.0f;
		body.AngularDamping = 4.0f;

		var yawRot = Rotation.From( new Angles( 0, Transform.Rotation.Angles().yaw, 0 ) );

		var worldMovement = yawRot * currentInput.movement;
		var velocityDirection = body.Velocity.WithZ( 0 );
		var velocityMagnitude = velocityDirection.Length;
		velocityDirection = velocityDirection.Normal;

		var velocityScale = (velocityMagnitude / LeanMaxVelocity).Clamp( 0, 1 );
		var leanDirection = worldMovement.LengthSquared == 0.0f
			? -velocityScale * velocityDirection
			: worldMovement;

		var targetUp = (Vector3.Up + leanDirection * LeanWeight * velocityScale).Normal;
		var currentUp = transform.NormalToWorld( Vector3.Up );
		var alignment = Math.Max( Vector3.Dot( targetUp, currentUp ), 0 );

		bool hasCollision = false;
		bool isGrounded = false;

		if ( !hasCollision || isGrounded )
		{
			var hoverForce = isGrounded && currentInput.throttle <= 0 ? Vector3.Zero : -1 * transform.NormalToWorld( Vector3.Up ) * -1700.0f;
			var movementForce = isGrounded ? Vector3.Zero : worldMovement * MovementAcceleration;
			var altitudeForce = transform.NormalToWorld( Vector3.Up ) * currentInput.throttle * AltitudeAcceleration;
			var totalForce = hoverForce + movementForce + altitudeForce;
			body.ApplyForce( (totalForce * alignment) * body.PhysicsBody.Mass );
		}

		if ( !hasCollision && !isGrounded )
		{
			var spinTorque = transform.NormalToWorld( new Vector3( 0, 0, -currentInput.yaw ) );
			spinTorque *= YawSpeed;

			var uprightTorque = Vector3.Cross( currentUp, targetUp ) * UprightSpeed;
			var uprightAlignment = alignment < UprightDot ? 0 : alignment;
			var totalTorque = spinTorque * alignment + uprightTorque * uprightAlignment;
			body.ApplyTorque( (totalTorque * alignment) * body.PhysicsBody.Mass );
		}
	}

	public override void OnDePossess()
	{
		CameraController.SetActive( false );
	}

	public override void OnPossess()
	{
		CameraController.SetActive( true );
	}

	bool IMinimapElement.IsVisible( Pawn viewer )
	{
		if ( Tags.Has( "invis" ) )
			return false;

		if ( HealthComponent.State == LifeState.Alive )
		{
			if ( (this as Pawn).IsPossessed )
				return false;
		}

		return viewer.Team == TeamComponent.Team;
	}

	public override Team Team => TeamComponent.Team;

	string ICustomMinimapIcon.CustomStyle => $"background-image-tint: {TeamComponent.Team.GetColor().Hex}";
	string IMinimapIcon.IconPath => "ui/icons/drone.png";

	Vector3 IMinimapElement.WorldPosition => Transform.Position;

	private struct DroneInputState
	{
		public Vector3 movement;
		public float throttle;
		public float pitch;
		public float yaw;

		public void Reset()
		{
			movement = Vector3.Zero;
			pitch = 0;
			yaw = 0;
		}
	}
}
