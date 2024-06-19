using Sandbox.Events;

namespace Facepunch;

/// <summary>
/// A weapon component that reacts to input actions.
/// </summary>
public abstract class InputWeaponComponent : EquipmentComponent,
	IGameEventHandler<EquipmentDeployedEvent>
{
	public enum InputListenerType
	{
		Pressed,
		Down,
		Released
	}

	/// <summary>
	/// What input action are we going to listen for?
	/// </summary>
	[Property, Category( "Base" )] public List<string> InputActions { get; set; } = new() { "Attack1" };
	
	/// <summary>
	/// Should we perform the action when ALL input actions match, or any?
	/// </summary>
	[Property, Category( "Base" )] public bool RequiresAllInputActions { get; set; }

	/// <summary>
	/// What kind of input are we listening for?
	/// </summary>
	[Property, Category( "Base" )] public InputListenerType InputType { get; set; } = InputListenerType.Down;

	/// <summary>
	/// ActionGraphs action so you can do stuff with visual scripting.
	/// </summary>
	[Property, Category( "Base" )] public Action<InputWeaponComponent> OnInputAction { get; set; }

	bool RunningWhileDeployed { get; set; }

	void IGameEventHandler<EquipmentDeployedEvent>.OnGameEvent( EquipmentDeployedEvent eventArgs )
	{
		if ( Equipment?.Owner?.IsLocallyControlled ?? false )
		{
			RunningWhileDeployed = InputActions.Any( x => Input.Down( x ) );
		}
	}

	/// <summary>
	/// Gets the input method
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public bool GetInputMethod( string action )
	{
		if ( RunningWhileDeployed ) return false;

		if ( InputType == InputListenerType.Pressed )
		{
			return Input.Pressed( action );
		}
		else if ( InputType == InputListenerType.Down )
		{
			return Input.Down( action );
		}
		
		return Input.Released( action );
	}

	bool isDown = false;

	protected bool IsDown() => isDown;

	/// <summary>
	/// Called when the input method succeeds.
	/// </summary>
	protected virtual void OnInput()
	{
		//
	}

	/// <summary>
	/// When the button is up
	/// </summary>
	protected virtual void OnInputUp()
	{
	}

	/// <summary>
	/// When the button is down
	/// </summary>
	protected virtual void OnInputDown()
	{
		//
	}

	protected override void OnFixedUpdate()
	{
		if ( !Equipment.IsValid() )
			return;
		
		// Don't execute weapon components on weapons that aren't deployed.
		if ( !Equipment.IsDeployed )
			return;
		
		if ( !Equipment.Owner.IsValid() )
			return;
		
		// We only care about input actions coming from the owning object.
		if ( !Equipment.Owner.IsLocallyControlled )
			return;

		if ( InputActions.All( x => !Input.Down( x ) ) )
		{
			RunningWhileDeployed = false;
		}

		bool matched = false;

		foreach ( var action in InputActions )
		{
			var success = GetInputMethod( action );

			if ( RequiresAllInputActions && !success )
			{
				matched = false;
				break;
			}
			if ( success )
			{
				matched = true;
			}
		}

		if ( matched )
		{
			OnInput();
			OnInputAction?.Invoke( this );

			if ( !isDown )
			{
				OnInputDown();
				isDown = true;
			}
		}
		else
		{
			if ( isDown )
			{
				OnInputUp();
				isDown = false;
			}
		}
	}
}
