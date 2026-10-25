using System.Text.Json;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Managers
{
    public static class CacheKeys
    {
        public const string PropertySearchGroup="property_search";
        public const string AdminDashboard="admin_dashboard";

        public static string Property(int propertyId)=>$"property_{propertyId}";
        public static string PropertySearch(SearchPropertyRequest request)=>$"property_search_{JsonSerializer.Serialize(request)}";
        public static string ReviewsGroup(int propertyId)=>$"reviews_{propertyId}";
        public static string Reviews(int propertyId,int pageNumber,int pageSize)=>$"reviews_{propertyId}_{pageNumber}_{pageSize}";
    }
}
