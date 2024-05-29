namespace Facepunch;

public static partial class GameObjectExtensions
{
	/// <summary>
	/// Take damage.
	/// </summary>
	/// <param name="go"></param>
	/// <param name="damage"></param>
	/// <param name="position"></param>
	/// <param name="force"></param>
	/// <param name="attackerId"></param>
	public static void TakeDamage( this GameObject go, float damage, Vector3 position, Vector3 force, Guid attackerId, Guid inflictorId = default )
	{
		foreach ( var damageable in go.Root.Components.GetAll<HealthComponent>( FindMode.EnabledInSelfAndDescendants ) )
		{
			damageable.TakeDamage( damage, position, force, attackerId, inflictorId );
		}
	}
}
