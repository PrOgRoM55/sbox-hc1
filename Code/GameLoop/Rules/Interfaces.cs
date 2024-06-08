﻿using System.Threading.Tasks;
using Facepunch;

public interface IGameEndCondition
{
	/// <summary>
	/// Called on the host at the end of a round to determine if the game should end.
	/// </summary>
	bool ShouldGameEnd();
}

public interface IRoundEndCondition
{
	/// <summary>
	/// Called on the host every update during a round to determine if it should end.
	/// </summary>
	bool ShouldRoundEnd();
}

public interface IGameStartListener
{
	/// <summary>
	/// Called on the host before <see cref="OnGameStart"/>.
	/// </summary>
	public void PreGameStart() { }

	/// <summary>
	/// Called on the host when a game is starting.
	/// </summary>
	public Task OnGameStart() => Task.CompletedTask;

	/// <summary>
	/// Called on the host after <see cref="OnGameStart"/> completes.
	/// </summary>
	public void PostGameStart() { }
}

public interface IGameEndListener
{
	/// <summary>
	/// Called on the host before <see cref="OnGameEnd"/>.
	/// </summary>
	public void PreGameEnd() { }

	/// <summary>
	/// Called on the host when a game is ending.
	/// </summary>
	public Task OnGameEnd() => Task.CompletedTask;

	/// <summary>
	/// Called on the host after <see cref="OnGameEnd"/> completes.
	/// </summary>
	public void PostGameEnd() { }
}

public interface IRoundStartListener
{
	/// <summary>
	/// Called on the host before <see cref="OnRoundStart"/>.
	/// </summary>
	public void PreRoundStart() { }

	/// <summary>
	/// Called on the host when a round is starting.
	/// </summary>
	public Task OnRoundStart() => Task.CompletedTask;

	/// <summary>
	/// Called on the host after <see cref="OnRoundStart"/> completes.
	/// </summary>
	public void PostRoundStart() { }
}

public interface IRoundEndListener
{
	/// <summary>
	/// Called on the host before <see cref="OnRoundEnd"/>.
	/// </summary>
	public void PreRoundEnd() { }

	/// <summary>
	/// Called on the host when a round is ending.
	/// </summary>
	public Task OnRoundEnd() => Task.CompletedTask;

	/// <summary>
	/// Called on the host after <see cref="OnRoundEnd"/> completes.
	/// </summary>
	public void PostRoundEnd() { }
}

public interface IPlayerSpawnListener
{
	/// <summary>
	/// Called on the host before <see cref="OnPlayerSpawn"/>.
	/// </summary>
	public void PrePlayerSpawn( PlayerController player ) { }

	/// <summary>
	/// Called on the host when <paramref name="player"/> respawns.
	/// </summary>
	public Task OnPlayerSpawn( PlayerController player ) => Task.CompletedTask;

	/// <summary>
	/// Called on the host after <see cref="OnPlayerSpawn"/>.
	/// </summary>
	public void PostPlayerSpawn( PlayerController player ) { }
}

public interface IPlayerJoinedListener
{
	/// <summary>
	/// Called on the host when a new player joins, before NetworkSpawn is called.
	/// </summary>
	public void OnConnect( PlayerController player ) { }

	/// <summary>
	/// Called on the host when a new player joins, after NetworkSpawn is called.
	/// </summary>
	public void OnJoined( PlayerController player ) { }
}

public interface ITeamAssignedListener
{
	/// <summary>
	/// Called on the host when a player is assigned to a team.
	/// </summary>
	public void OnTeamAssigned( PlayerController player, Team team ) { }
}

public interface ITeamSwapListener
{
	/// <summary>
	/// Called on the host when both teams swap.
	/// </summary>
	public void OnTeamSwap() { }
}