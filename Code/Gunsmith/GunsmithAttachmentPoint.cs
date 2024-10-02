using Sandbox;
using static Sandbox.Clothing;

namespace Facepunch.Gunsmith;

/// <summary>
/// We'll use this for gunsmith weapons, to designate where we can display or attach something to a weapon.
/// </summary>
public partial class GunsmithAttachmentPoint : Component
{
	[Property]
	public GunsmithPartType Category { get; set; }

	[Property]
	public GameObject CurrentGameObject { get; set; }

	[Property]
	public GunsmithPart CurrentPart { get; set; }

	/// <summary>
	/// Set the gunsmith part to this part
	/// </summary>
	/// <param name="part"></param>
	public void SetPart( GunsmithPart part )
	{
		if ( CurrentGameObject.IsValid() )
		{
			CurrentGameObject.Destroy();
		}

		CurrentPart = part;

		if ( CurrentPart is null )
		{
			CurrentGameObject = null;
			ResetBodygroups();

			return;
		}

		if ( CurrentPart.MainPrefab.IsValid() )
		{
			CurrentGameObject = CurrentPart.MainPrefab.Clone( new CloneConfig()
			{
				Transform = new(),
				Parent = GameObject,
				StartEnabled = true,
			} );

			var attachment = CurrentGameObject.GetComponent<GunsmithAttachment>();
			if ( attachment.IsValid() )
			{
				attachment.Initialize( this );
			}
		}

		if ( CurrentPart.ModifyBodygroups )
		{
			ApplyBodygroups();
		}
	}

	private void ApplyBodygroups()
	{
		var renderer = GetComponentInParent<SkinnedModelRenderer>();

		foreach ( var kv in CurrentPart.Bodygroups )
		{
			renderer.SetBodyGroup( kv.Key, kv.Value );
		}
	}

	private void ResetBodygroups()
	{
		var renderer = GetComponentInParent<SkinnedModelRenderer>();

		foreach ( var bodyPart in renderer.Model.BodyParts )
		{
			renderer.SetBodyGroup( bodyPart.Name, 0 );
		}
	}
}
