namespace Facepunch;

public partial class PlayerController
{
	/// <summary>
	/// Sync the player's steamid
	/// </summary>
	[Sync] public ulong SteamId { get; set; }

	/// <summary>
	/// A shorthand accessor to say if we're controlling this player.
	/// </summary>
	public bool IsLocallyControlled => IsViewer && !IsProxy && !IsBot;

	/// <summary>
	/// Is this player the currently possessed controller
	/// </summary>
	public bool IsViewer => (this as IPawn).IsPossessed;

	/// <summary>
	/// What should this player be called?
	/// </summary>
	/// <returns></returns>
	public string GetPlayerName() => IsBot ? $"BOT {BotManager.Instance.GetName( BotId )}" : Network.OwnerConnection?.DisplayName ?? "";

	/// <summary>
	/// Called when possessed.
	/// </summary>
	public void OnPossess()
	{
		SetupCamera();
	}

	public void TryDePossess()
	{
		if ( !IsLocallyControlled ) return;
		(this as IPawn).DePossess();
	}

	private void SetupCamera()
	{
		// if we're spectating a remote player, use the camera mode preference
		// otherwise: first person for now
		var spectateSystem = SpectateSystem.Instance;
		if ( spectateSystem is not null && (IsProxy || IsBot) )
		{
			CameraController.Mode = spectateSystem.CameraMode;
		}
		else
		{
			CameraController.Mode = CameraMode.FirstPerson;
		}

		CameraController.SetActive( true );
	}

	public void OnDePossess()
	{
		CameraController.SetActive( false );
	}
}