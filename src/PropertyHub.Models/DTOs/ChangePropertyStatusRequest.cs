namespace PropertyHub.Models.DTOs;

using PropertyHub.DataAccess.Enums;

public class ChangePropertyStatusRequest
{
    public PropertyStatus Status { get; set; }
}