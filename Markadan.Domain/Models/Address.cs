namespace Markadan.Domain.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string AddressName { get; set; } = "";
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string Country { get; set; } = "";


        //np
        public int AppUserId { get; set; }
        public required AppUser AppUser { get; set; }
    }
}
