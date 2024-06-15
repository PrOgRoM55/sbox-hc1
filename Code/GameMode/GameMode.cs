﻿using Sandbox.Events;

namespace Facepunch;

public record GamemodeInitializedEvent( string Title ) : IGameEvent;

/// <summary>
/// Handles the main game loop, using components that listen to state change
/// events to handle game logic.
/// </summary>
public sealed partial class GameMode : SingletonComponent<GameMode>, Component.INetworkListener
{
	/// <summary>
	/// Path in the scene of the game mode selected by the host.
	/// </summary>
	public static string ActivePath { get; set; }

	[Property]
	public string Title { get; set; }

	[Property]
	public string Description { get; set; }

	private StateMachineComponent _stateMachine;

	public StateMachineComponent StateMachine => _stateMachine ??= Components.GetInDescendantsOrSelf<StateMachineComponent>();

	protected override void OnAwake()
	{
		// Only stay enabled if host chose this game mode

		if ( ActivePath is { } path && !path.Equals( GameObject.GetScenePath(), StringComparison.OrdinalIgnoreCase ) )
		{
			GameObject.Enabled = false;
			return;
		}

		// Fallback for testing in editor - just use first active game mode

		if ( Instance is { IsValid: true, Active: true, Scene: {} scene } && scene == Scene )
		{
			Log.Info( $"A GameMode is already active, disabling {GameObject.GetScenePath()}" );
			GameObject.Enabled = false;
			return;
		}

		Scene.Dispatch( new GamemodeInitializedEvent( Title ) );

		base.OnAwake();
	}

	void INetworkListener.OnBecameHost( Connection previousHost )
	{
		Log.Info( "We became the host, taking over the game loop..." );
	}

	private StateComponent _prevState;
	private readonly Dictionary<Type, Component> _componentCache = new();

	/// <summary>
	/// Gets the given component from within the game mode's object hierarchy, or null if not found / enabled.
	/// </summary>
	public T Get<T>( bool required = false )
		where T : class
	{
		if ( _prevState != StateMachine.CurrentState )
		{
			_prevState = StateMachine.CurrentState;
			_componentCache.Clear();
		}

		if ( !_componentCache.TryGetValue( typeof(T), out var component ) || component is { IsValid: false } || component is { Active: false } )
		{
			component = Components.GetInDescendantsOrSelf<T>() as Component;
			_componentCache[typeof(T)] = component;
		}

		if ( required && component is not T )
		{
			throw new Exception( $"Expected a {typeof(T).Name} to be active in the {nameof(GameMode)}!" );
		}

		return component as T;
	}

	/// <summary>
	/// RPC called by a client when they have finished respawning.
	/// </summary>
	[Authority]
	public void SendSpawnConfirmation( Guid playerGuid )
	{
		var player = Scene.Directory.FindComponentByGuid( playerGuid ) as PlayerController
			?? throw new Exception( $"Unknown {nameof(PlayerController)} Id: {playerGuid}" );

		Scene.Dispatch( new PlayerSpawnedEvent( player ) );
	}
}
