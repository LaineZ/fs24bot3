namespace fs24bot3.Models
{
    public class ItemInventory
    {
        public class Shop
        {
            public string Name { get; set; }
            public int Price { get; set; }
            public string Slug { get; set; }
            public bool Sellable { get; set; }
        }
    }
}