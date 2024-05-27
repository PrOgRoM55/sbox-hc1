namespace Facepunch;

public enum Team
{
    Unassigned = 0,
    Terrorist,
    CounterTerrorist
}

/// <summary>
/// Designates the team for a player, or an object. Mostly for players.
/// </summary>
public partial class TeamComponent : Component
{
    /// <summary>
    /// An action (for ActionGraph, or other components) to listen for team changes
    /// </summary>
    [Property, Group( "Actions" )] public Action<Team, Team> OnTeamChanged { get; set; }

    private Team team;
    /// <summary>
    /// The GameObject's team
    /// </summary>
    [Property, Group( "Setup" )]
    public Team Team
    {
        get => team;
        set
        {
            if ( team == value ) return;

            var before = team;
            team = value;
            TeamChanged( before, team );
        }
    }

    /// <summary>
    /// Called when <see cref="Team"/> changes
    /// </summary>
    /// <param name="before"></param>
    /// <param name="after"></param>
    private void TeamChanged( Team before, Team after )
    {
	    OnTeamChanged?.Invoke( before, after );
    }
}

public static class TeamExtensions
{
    /// <summary>
    /// Accessor to get the team of someone/something.
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    public static Team GetTeam( this GameObject gameObject )
    {
        var comp = gameObject.Components.Get<TeamComponent>( FindMode.EverythingInSelfAndAncestors );
        if ( !comp.IsValid() ) return Team.Unassigned;

        return comp.Team;
    }

    //
    // For all of this, maybe the gamemode should be controlling it, and not some global methods
    //

    /// <summary>
    /// Are we friendly with this other team?
    /// </summary>
    /// <param name="teamOne"></param>
    /// <param name="teamTwo"></param>
    /// <returns></returns>
    private static bool IsFriendly( Team teamOne, Team teamTwo )
    {
        if ( teamOne == Team.Unassigned || teamTwo == Team.Unassigned ) return false;
        return teamOne == teamTwo;
    }

    /// <summary>
    /// Are these two GameObjects friends with eachother?
    /// </summary>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsFriendly( this GameObject self, GameObject other )
    {
        return IsFriendly( self.GetTeam(), other.GetTeam() );
    }
}