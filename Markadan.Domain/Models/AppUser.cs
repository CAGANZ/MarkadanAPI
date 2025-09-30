using Microsoft.AspNetCore.Identity;

namespace Markadan.Domain.Models
{
    public class AppUser : IdentityUser<int>
    {
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string GovId { get; set; }
        public required DateTime Birthday { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    }
}
