using Sandbox.Network;
using System.Threading.Tasks;

namespace Facepunch;

public sealed class GameNetworkManager : SingletonComponent<GameNetworkManager>, Component.INetworkListener
{
	/// <summary>
	/// Which player prefab should we spawn?
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// Is this game multiplayer? If not, we won't create a lobby.
	/// </summary>
	[Property] public bool IsMultiplayer { get; set; } = true;

	protected override void OnStart()
	{
		if ( !IsMultiplayer ) return;

		//
		// Create a lobby if we're not connected
		//
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}
	}

	[DeveloperCommand( "Add Bot" )]
	private static void Command_Add_Bot()
	{
		var player = Instance.PlayerPrefab.Clone();
		player.NetworkSpawn();
	}

	/// <summary>
	/// Called when a network connection becomes active
	/// </summary>
	/// <param name="channel"></param>
	public void OnActive( Connection channel )
	{
		if ( !IsMultiplayer ) return;

		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );

		var player = PlayerPrefab.Clone( GameMode.Instance.GetSpawnTransform( Team.Unassigned ) );
		player.NetworkSpawn( channel );

		var playerComponent = player.Components.Get<PlayerController>();

		if ( playerComponent.IsValid() )
		{
			playerComponent.NetPossess();

			if ( playerComponent.CanRespawn )
			{
				playerComponent.Respawn();
			}
		}
	}

	[ConCmd( "_lobby_list" )]
	public static void LobbyList()
	{
		QueryLobbies();
	}

	[ConCmd( "_lobby_join" )]
	public static void JoinAnyLobby()
	{
		AsyncJoinAnyLobby();
	}

	private static void QueryLobbies()
	{
		_ = AsyncGetLobbies();
	}

	static async Task<List<LobbyInformation>> AsyncGetLobbies()
	{
		var lobbies = await Networking.QueryLobbies( Game.Ident );

		foreach ( var lob in lobbies )
		{
			Log.Info( $"{lob.Name}'s lobby ({lob.Members}/{lob.MaxMembers})" );
		}

		return lobbies;
	}

	static async void AsyncJoinAnyLobby()
	{
		var lobbies = await AsyncGetLobbies();

		try
		{
			var lobby = lobbies.First( x => !x.IsFull );
			if ( await GameNetworkSystem.TryConnectSteamId( lobby.LobbyId ) )
			{
				Log.Info("joined lobby!");
			}
		}
		catch
		{
			Log.Warning( "No available lobbies" );
		}
	}
}
