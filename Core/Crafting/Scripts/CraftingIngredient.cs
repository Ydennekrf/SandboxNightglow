using Godot;

namespace ethra.V1
{
	/// <summary>
	/// Static item requirement inside a CraftingRecipe resource.
	/// </summary>
	[GlobalClass]
	public partial class CraftingIngredient : Resource
	{
		/// <summary>Numeric item ID consumed by the recipe.</summary>
		[Export] public int ItemId { get; set; }
		/// <summary>Quantity of this item required for one craft.</summary>
		[Export] public int Quantity { get; set; } = 1;
	}
}
