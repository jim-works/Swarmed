using Godot;
using System.Collections.Generic;

public class CraftingRecipeUI : Control
{
    [Export]
    public PackedScene ItemSlotUI;
    [Export]
    public int Padding = 2;
    [Export]
    public int SlotSizePx = 64;
    private List<ItemSlotUI> slots = new List<ItemSlotUI>();
    private Player player;
    private Recipe displaying;

    private float getHeight(int rows) => rows*SlotSizePx+(rows+1)*Padding;
    
    //returns the height of the container after displaying everything
    //TODO: pooling
    public float DisplayRecipe(Recipe r)
    {
        player = World.Singleton.Players[0];
        displaying = r;
        foreach (var slot in slots)
        {
            slot.QueueFree();
        }
        slots.Clear();
        Control ingredientsLabel = GetNode<Label>("IngredientsLabel");
        Control productsLabel = GetNode<Label>("ProductsLabel");
        //ingredients
        float offsetY = ingredientsLabel.RectSize.y+Padding;
        int endrow = displayList(r.Ingredients, new Vector2(0,offsetY), 0);
        //products
        productsLabel.RectPosition = new Vector2(productsLabel.RectPosition.x, offsetY+getHeight(endrow+1)+Padding);
        offsetY += productsLabel.RectSize.y+Padding*2;
        endrow = displayList(r.Product, new Vector2(0,offsetY), endrow+1, (b) => craft());
        RectSize = new Vector2(RectSize.x, offsetY + getHeight(endrow+1)+Padding);
        return RectSize.y;
    }

    private int displayList(List<ItemStack> items, Vector2 offset, int startRow, System.Action<int> onclick=null)
    {
        int slotsPerRow = Mathf.Max((int)RectSize.x/(SlotSizePx+Padding),1);
        int row = startRow-1;
        //ingredients
        for (int i = 0; i < items.Count; i++)
        {
            int column = i % slotsPerRow;
            if (column == 0) row++;
            ItemSlotUI slot = ItemSlotUI.Instance<ItemSlotUI>();
            AddChild(slot);
            slot.RectPosition = new Vector2(offset.x+column*SlotSizePx+(column+1)*Padding, offset.y+getHeight(row));
            int idx = i;
            if (onclick != null) slot.OnClick += onclick;
            slot.DisplayItem(items[i]);
            slots.Add(slot);
        }
        return row;
    }
    private void craft()
    {
        //temporary
        if (player.MouseInventory.Items[0].Size == 0) displaying.Craft(player.Inventory, player.MouseInventory);
    }
}