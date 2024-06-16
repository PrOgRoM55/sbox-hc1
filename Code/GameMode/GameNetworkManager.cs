using Sandbox.Network;
using System.Threading.Tasks;
using Sandbox.Events;

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
		PlayerId.Init();

		if ( !IsMultiplayer )
		{
			var player = PlayerPrefab.Clone();

			var playerComponent = player.Components.Get<PlayerController>();
			if ( !playerComponent.IsValid() )
				return;

			OnPlayerJoined( playerComponent, Connection.Local );

			return;
		}

		//
		// Create a lobby if we're not connected
		//
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}
	}

	/// <summary>
	/// Called when a network connection becomes active
	/// </summary>
	/// <param name="channel"></param>
	public void OnActive( Connection channel )
	{
		if ( !IsMultiplayer ) return;

		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );

		var player = PlayerPrefab.Clone();

		var playerComponent = player.Components.Get<PlayerController>();
		if ( !playerComponent.IsValid() )
			return;

		OnPlayerJoined( playerComponent, channel );
	}


	public void OnPlayerJoined( PlayerController player, Connection channel )
	{
		Scene.Dispatch( new PlayerConnectedEvent( player ) );

		var spawnPoint = GameUtils.GetRandomSpawnPoint( player.TeamComponent.Team );
		player.Teleport( spawnPoint );
		player.Initialize();
		player.GameObject.NetworkSpawn( channel );

		Scene.Dispatch( new PlayerJoinedEvent( player ) );
		
		if ( player.HealthComponent.State == LifeState.Alive )
			GameMode.Instance?.SendSpawnConfirmation( player.Id );
	}

	[ConCmd( "lobby_list" )]
	public static void LobbyList()
	{
		QueryLobbies();
	}

	[ConCmd( "lobby_join" )]
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
			Log.Info( $"{lob.Name}'s lobby [{lob.LobbyId}] ({lob.Members}/{lob.MaxMembers})" );
		}

		return lobbies;
	}

	static async void AsyncJoinAnyLobby()
	{
		var lobbies = await AsyncGetLobbies();

		try
		{
			var lobby = lobbies.OrderByDescending( x => x.Members ).First( x => !x.IsFull );
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