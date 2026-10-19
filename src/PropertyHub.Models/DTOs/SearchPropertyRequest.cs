namespace PropertyHub.Models.DTOs
{
    public class SearchPropertyRequest
    {
        public string City{get;set;}
        public string State{get;set;}
        public string? searchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinBedrooms { get; set; }
        public int? MaxBedrooms { get; set; }
        public int? MinBathrooms { get; set; }
        public int? MaxBathrooms { get; set; }
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortOrder { get; set; } = "desc";
    }
}