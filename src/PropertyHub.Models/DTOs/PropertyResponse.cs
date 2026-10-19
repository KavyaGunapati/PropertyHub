using PropertyHub.DataAccess.Enums;

namespace PropertyHub.Models.DTOs
{
    public class PropertyResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public decimal Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int SquareFeet { get; set; }
        public int YearBuilt { get; set; }
        public PropertyStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int AgentId {get;set;}
        public string AgentName{get;set;}
        public List<PropertyImageResponse> Images { get; set; }
    }
    public class PropertyImageResponse
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
    }
}