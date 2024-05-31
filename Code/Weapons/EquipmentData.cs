﻿using Facepunch;

public abstract class EquipmentData
{
	public string Name { get; protected set; }
	public string Icon { get; protected set; }
	public virtual int GetPrice( PlayerController player ) => 0;
	public virtual bool IsOwned( PlayerController player ) => true;
	public virtual bool IsVisible( PlayerController player ) => true;

	protected virtual void OnPurchase( PlayerController player ) { }

	public void Purchase( PlayerController player )
	{
		if ( !IsOwned( player ) )
		{
			player.Inventory.Balance -= GetPrice( player );
			OnPurchase( player );
		}
	}

	public static IEnumerable<EquipmentData> GetAll()
	{
		return new List<EquipmentData>
		{
			new ArmorEquipment("Kevlar", "/ui/equipment/armor.png"),
			new ArmorWithHelmetEquipment("Kevlar + Helmet", "/ui/equipment/helmet.png"),
			new DefuseKitEquipment("Defuse Kit", "/ui/equipment/defusekit.png")
		};
	}
}

public class ArmorEquipment : EquipmentData
{
	public ArmorEquipment( string name, string icon )
	{
		Name = name;
		Icon = icon;
	}

	public override int GetPrice( PlayerController player ) => 650;

	protected override void OnPurchase( PlayerController player )
	{
		player.HealthComponent.Armor = 100;
	}

	public override bool IsOwned( PlayerController player ) => player.HealthComponent.Armor == 100;
}

public class ArmorWithHelmetEquipment : EquipmentData
{
	public ArmorWithHelmetEquipment( string name, string icon )
	{
		Name = name;
		Icon = icon;
	}

	public override int GetPrice( PlayerController player )
	{
		if ( player.HealthComponent.Armor == 100 )
			return 350;

		return 1000;
	}

	protected override void OnPurchase( PlayerController player )
	{
		player.HealthComponent.Armor = 100;
		player.HealthComponent.HasHelmet = true;
	}

	public override bool IsOwned( PlayerController player )
	{
		return player.HealthComponent.Armor == 100 && player.HealthComponent.HasHelmet;
	}
}

public class DefuseKitEquipment : EquipmentData
{
	public DefuseKitEquipment( string name, string icon )
	{
		Name = name;
		Icon = icon;
	}

	public override int GetPrice( PlayerController player ) => 400;

	protected override void OnPurchase( PlayerController player )
	{
		player.Inventory.HasDefuseKit = true;
	}

	public override bool IsVisible( PlayerController player ) => player.GameObject.GetTeam() == Team.CounterTerrorist;
}