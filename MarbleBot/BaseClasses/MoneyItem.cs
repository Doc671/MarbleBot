namespace MarbleBot
{
    public struct MoneyItem // Class for items when using money-based commands
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public bool OnSale { get; set; }
    }
}