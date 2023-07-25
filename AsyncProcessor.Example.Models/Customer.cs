namespace AsyncProcessor.Example.Models
{
    public class Customer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IDictionary<string, Address> Addresses { get; set; }
    }
}