using Dukan.Web.Application.DTOs;
using Dukan.Web.Domain.Entities;

namespace Dukan.Web.Application.Mapper;

public static class CustomerMapper
{

    public static CustomerDto ToDto(this Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            StoreName = customer.StoreName,
            Phone = customer.Phone,
            WhatsAppNumber = customer.WhatsAppNumber,
            Notes = customer.Notes,
            CreatedAt = customer.CreatedAt,
            Subscriptions = customer.Subscriptions.Select(s => s.ToDto()).ToList()
        };
    }
}
