﻿using Facepunch;
using System.Threading.Tasks;

/// <summary>
/// Handles the main game loop, using components that listen to state change
/// events to handle game logic.
/// </summary>
public sealed class GameMode : SingletonComponent<GameMode>
{
	/// <summary>
	/// Current game state.
	/// </summary>
	[Property, Sync]
	public GameState State { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		// Add essential components for required interfaces

		if ( Components.Get<ISpawnPointAssigner>() is null )
		{
			Components.Create<AnySpawnAssigner>();
		}
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
		{
			return;
		}

		_ = ResumeGame();
	}

	public Task ResumeGame()
	{
		return GameLoop();
	}

	private async Task GameLoop()
	{
		while ( State != GameState.Ended )
		{
			switch ( State )
			{
				case GameState.PreGame:
					await StartGame();

					State = GameState.PreRound;
					break;

				case GameState.PreRound:
					await StartRound();

					State = GameState.DuringRound;
					break;

				case GameState.DuringRound:
					await Task.FixedUpdate();

					State = ShouldRoundEnd()
						? GameState.PostRound
						: GameState.DuringRound;
					break;

				case GameState.PostRound:
					await EndRound();

					State = ShouldGameEnd()
						? GameState.PostGame
						: GameState.PreRound;
					break;

				case GameState.PostGame:
					await EndGame();

					State = GameState.Ended;
					break;
			}
		}
	}

	private Task StartGame()
	{
		return Dispatch<IGameStartListener>(
			x => x.PreGameStart(),
			x => x.OnGameStart(),
			x => x.PostGameStart() );
	}

	private Task StartRound()
	{
		return Dispatch<IRoundStartListener>(
			x => x.PreRoundStart(),
			x => x.OnRoundStart(),
			x => x.PostRoundStart() );
	}

	private Task EndRound()
	{
		return Dispatch<IRoundEndListener>(
			x => x.PreRoundEnd(),
			x => x.OnRoundEnd(),
			x => x.PostRoundEnd() );
	}

	private Task EndGame()
	{
		return Dispatch<IGameEndListener>(
			x => x.PreGameEnd(),
			x => x.OnGameEnd(),
			x => x.PostGameEnd() );
	}

	private bool ShouldGameEnd()
	{
		return Components.GetAll<IGameEndCondition>()
			.Any( x => x.ShouldGameEnd() );
	}

	private bool ShouldRoundEnd()
	{
		return Components.GetAll<IRoundEndCondition>()
			.Any( x => x.ShouldRoundEnd() );
	}

	public Transform GetSpawnTransform( Team team )
	{
		return GetSingleOrThrow<ISpawnPointAssigner>()
			.GetSpawnTransform( team );
	}

	/// <summary>
	/// RPC called by a client when they have finished respawning.
	/// </summary>
	[Authority]
	public void HandlePlayerSpawn()
	{
		var player = GameUtils.AllPlayers.FirstOrDefault( x => x.Network.OwnerConnection == Rpc.Caller )
			?? throw new Exception( $"Unable to find {nameof(PlayerController)} owned by {Rpc.Caller.DisplayName}." );

		_ = HandlePlayerSpawn( player );
	}

	private Task HandlePlayerSpawn( PlayerController player )
	{
		return Dispatch<IPlayerSpawnListener>(
			x => x.PrePlayerSpawn( player ),
			x => x.OnPlayerSpawn( player ),
			x => x.PostPlayerSpawn( player ) );
	}

	private async Task Dispatch<T>( Action<T> pre, Func<T, Task> on, Action<T> post )
	{
		var components = Components.GetAll<T>().ToArray();

		foreach (var comp in components)
		{
			pre( comp );
		}

		await Task.WhenAll( Components.GetAll<T>().Select( on ) );

		foreach (var comp in components)
		{
			post( comp );
		}
	}

	private T GetSingleOrThrow<T>()
	{
		return Components.GetInDescendantsOrSelf<T>()
			?? throw new Exception( $"Missing required component {typeof( T ).Name}." );
	}
}
