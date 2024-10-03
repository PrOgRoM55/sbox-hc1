namespace Facepunch.Gunsmith;

/// <summary>
/// The manager for gunsmith, we'll place this in the gunsmith scene and handle state.
/// </summary>
public partial class GunsmithSystem : Component
{
	[Property]
	public EquipmentResource Equipment { get; set; }

	[Property]
	public SkinnedModelRenderer Renderer { get; set; }

	[Property]
	public GunsmithController Controller { get; set; }

	public GunsmithWeapon Weapon => Controller.Weapon;

	protected override void OnStart()
	{
		var inst = Equipment.ViewModelPrefab.Clone( new CloneConfig()
		{
			Name = "Runtime Weapon",
			Parent = GameObject,
			StartEnabled = true,
			Transform = new()
		} );

		Renderer = inst.GetComponent<SkinnedModelRenderer>();
		Renderer.UseAnimGraph = false;
		Controller.Weapon = inst.GetComponent<GunsmithWeapon>();
	}
}
