﻿using Facepunch;

public sealed class BombDefusalScenario : Component,
	IGameStartListener,
	IRoundStartListener,
	IBombPlantedListener,
	IBombDetonatedListener,
	IBombDefusedListener,
	IRoundEndListener,
	IRoundEndCondition,
	ITeamAssignedListener,
	ITeamSwapListener
{
	[RequireComponent] public RoundTimeLimit RoundTimeLimit { get; private set; }
	[RequireComponent] public TeamEliminated TeamEliminated { get; private set; }
	[RequireComponent] public TeamScoring TeamScoring { get; private set; }

	[Property, HostSync, Category( "Economy" )]
	public int StartMoney { get; set; } = 800;

	[Property, HostSync, Category( "Economy" )]
	public int DefaultWinTeamIncome { get; set; } = 3250;

	[Property, HostSync, Category( "Economy" )]
	public int BombDefusedTeamIncome { get; set; } = 3500;

	[Property, HostSync, Category( "Economy" )]
	public int BombDetonatedTeamIncome { get; set; } = 3500;

	[Property, HostSync, Category( "Economy" )]
	public int BaseLossTeamIncome { get; set; } = 1400;

	/// <summary>
	/// How much each team's loss income increases per loss streak level.
	/// </summary>
	[Property, HostSync, Category( "Economy" )]
	public int LossBonusIncrement { get; set; } = 500;

	[Property, HostSync, Category( "Economy" )]
	public int MaxLossStreakLevel { get; set; } = 4;

	[HostSync]
	public NetDictionary<Team, int> LossStreakLevel { get; private set; } = new();

	[HostSync] public bool IsBombPlanted { get; private set; }
	[HostSync] public bool BombHasDetonated { get; private set; }
	[HostSync] public bool BombWasDefused { get; private set; }

	private int GetLossStreakBonus( Team team )
	{
		if ( !LossStreakLevel.TryGetValue( team, out var level ) )
		{
			level = 0;
		}

		return BaseLossTeamIncome + level * LossBonusIncrement;
	}

	private void IncrementLossStreak( Team team, int sign )
	{
		LossStreakLevel[team] = Math.Clamp( LossStreakLevel.GetValueOrDefault( team ) + sign, 0, MaxLossStreakLevel );
	}

	void IGameStartListener.PostGameStart()
	{
		LossStreakLevel.Clear();
	}

	void ITeamSwapListener.OnTeamSwap()
	{
		LossStreakLevel.Clear();
	}

	void ITeamAssignedListener.OnTeamAssigned( PlayerController player, Team team )
	{
		player.Inventory.Clear();
		player.Inventory.SetCash( StartMoney );
	}

	void IRoundStartListener.PostRoundStart()
	{
		GameMode.Instance.ShowStatusText( Team.Terrorist, "Plant the Bomb" );
		GameMode.Instance.ShowStatusText( Team.CounterTerrorist, "Defend" );
	}

	void IBombPlantedListener.OnBombPlanted( PlayerController planter, GameObject bomb, BombSite bombSite )
	{
		IsBombPlanted = true;
		BombHasDetonated = false;
		BombWasDefused = false;

		RoundTimeLimit.Enabled = false;
		TeamEliminated.IgnoreTeam = Team.Terrorist;

		GameMode.Instance.ShowStatusText( Team.Terrorist, "Defend" );
		GameMode.Instance.ShowStatusText( Team.CounterTerrorist, "Defuse the Bomb" );
		GameMode.Instance.HideTimer();
	}

	void IBombDetonatedListener.OnBombDetonated( GameObject bomb, BombSite bombSite )
	{
		BombHasDetonated = true;
	}

	void IBombDefusedListener.OnBombDefused( PlayerController defuser, GameObject bomb, BombSite bombSite )
	{
		BombWasDefused = true;
	}

	void IRoundEndListener.PreRoundEnd()
	{
		if ( TeamScoring.RoundWinner == Team.Terrorist )
		{
			IncrementLossStreak( Team.Terrorist, -1 );
			IncrementLossStreak( Team.CounterTerrorist, 1 );

			GameUtils.GiveTeamIncome( Team.Terrorist, IsBombPlanted ? BombDetonatedTeamIncome : DefaultWinTeamIncome );
			GameUtils.GiveTeamIncome( Team.CounterTerrorist, GetLossStreakBonus( Team.CounterTerrorist ) );
		}
		else if ( TeamScoring.RoundWinner == Team.CounterTerrorist )
		{
			IncrementLossStreak( Team.Terrorist, 1 );
			IncrementLossStreak( Team.CounterTerrorist, -1 );

			GameUtils.GiveTeamIncome( Team.Terrorist, GetLossStreakBonus( Team.Terrorist ) );
			GameUtils.GiveTeamIncome( Team.CounterTerrorist, BombWasDefused ? BombDefusedTeamIncome : DefaultWinTeamIncome );
		}

		IsBombPlanted = false;

		RoundTimeLimit.Enabled = true;
		TeamEliminated.IgnoreTeam = Team.Unassigned;
	}

	public bool ShouldRoundEnd()
	{
		if ( !IsBombPlanted )
		{
			return false;
		}

		if ( BombWasDefused )
		{
			TeamScoring.RoundWinner = Team.CounterTerrorist;
			return true;
		}

		if ( BombHasDetonated )
		{
			TeamScoring.RoundWinner = Team.Terrorist;
			return true;
		}

		return false;
	}
}
