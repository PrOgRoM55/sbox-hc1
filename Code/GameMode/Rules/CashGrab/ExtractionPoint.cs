using Sandbox.Events;

namespace Facepunch;

public record CashPointBagExtractedEvent( PlayerController Player, ExtractionPoint ExtractionPoint ) : IGameEvent;

/// <summary>
/// An extraction point, associated with a <see cref="CashPoint"/>
/// </summary>
[EditorHandle( "ui/Minimaps/cashgrab/extract.png" )]
public partial class ExtractionPoint : Component, ICustomMinimapIcon, Component.ITriggerListener, IMarkerObject
{
	/// <summary>
	/// Our cash point
	/// </summary>
	[Property] public CashPoint CashPoint { get; set; } 

	/// <summary>
	/// The extraction point's trigger.
	/// </summary>
	[Property] public Collider Trigger { get; set; }

	string IMinimapIcon.IconPath => "ui/minimaps/cashgrab/extract.png";

	Vector3 IMinimapElement.WorldPosition => Transform.Position;

	string ICustomMinimapIcon.CustomStyle
	{
		get
		{
			var color = Color.Green;
			return $"background-tint: {color.Hex};";
		}
	}

	bool ShouldShow()
	{
		return CashPoint.State == CashPoint.CashPointState.Open;
	}

	bool IMinimapElement.IsVisible( IPawn viewer ) => ShouldShow();

	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		var tracker = GameMode.Instance.Get<CashPointTracker>();

		if ( CashPoint.State == CashPoint.CashPointState.Open )
		{
			var player = other.GameObject.Root.Components.Get<PlayerController>( FindMode.EnabledInSelfAndDescendants );
			if ( player.IsValid() )
			{
				var inventory = player.Inventory;
				if ( inventory.Has( tracker.Resource ) )
				{
					inventory.Remove( tracker.Resource );
					Scene.Dispatch( new CashPointBagExtractedEvent( player, this ) );
				}
			}

			// TODO: tag the dropped weapon's player
			//var cash = other.GameObject.Root.Components.Get<CashBag>( FindMode.EnabledInSelfAndDescendants );
			//if ( cash.IsValid() )
			//{
			//	cash.GameObject.Destroy();
			//	Scene.Dispatch( new CashPointBagExtractedEvent( player, this ) );
			//}
		}
	}

	/// <summary>
	/// Where is the marker?
	/// </summary>
	Vector3 IMarkerObject.MarkerPosition => Transform.Position + Vector3.Up * 32f;

	/// <summary>
	/// What icon?
	/// </summary>
	string IMarkerObject.MarkerIcon => "/ui/minimaps/cashgrab/extract.png";

	/// <summary>
	/// What text?
	/// </summary>
	string IMarkerObject.DisplayText => "Extraction Point";

	/// <summary>
	/// Should we show this marker?
	/// </summary>
	/// <returns></returns>
	bool IMarkerObject.ShouldShow() => ShouldShow();

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Green;
		var box = BBox.FromPositionAndSize( Vector3.Zero, 64 );
		Gizmo.Hitbox.BBox( box );
		Gizmo.Draw.LineBBox( box );
	}
}
