using Microsoft.AspNetCore.Identity;

namespace Markadan.Domain.Models
{
    public class AppUser : IdentityUser<int>
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string GovId { get; set; }
        public DateTime Birthday { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    }
}
