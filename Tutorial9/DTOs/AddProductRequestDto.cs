namespace Tutorial9.DTOs

{
    public class AddProductRequestDto
    {
        public int IdProduct { get; set; }
        public int IdWarehouse { get; set; }
        public int Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddProductResponseDto
    {
        public int NewId { get; set; }
    }
}