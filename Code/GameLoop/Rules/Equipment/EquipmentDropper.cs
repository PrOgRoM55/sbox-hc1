using Sandbox.Events;

namespace Facepunch.GameRules;

/// <summary>
/// Players drop their held weapon when killed.
/// </summary>
public partial class EquipmentDropper : Component,
	IGameEventHandler<KillEvent>
{
	/// <summary>
	/// Can we drop this weapon?
	/// </summary>
	/// <param name="weapon"></param>
	/// <returns></returns>
	private bool CanDrop( Weapon weapon )
	{
		if ( GameMode.Instance.Get<DefaultEquipment>()?.Weapons.Contains( weapon.Resource ) is true )
			return false;

		if ( weapon.Resource.Slot == WeaponSlot.Melee ) 
			return false;

		return true;
	}

	void IGameEventHandler<KillEvent>.OnGameEvent( KillEvent eventArgs )
	{
		var player = GameUtils.GetPlayerFromComponent( eventArgs.DamageInfo.Victim );
		if ( !player.IsValid() )
			return;

		var droppables = player.Inventory.Weapons
			.Where( CanDrop )
			.ToList();

		for ( var i = droppables.Count - 1; i >= 0; i-- )
		{
			player.Inventory.DropWeapon( droppables[i].Id );
		}

		player.Inventory.Clear();
	}
}
