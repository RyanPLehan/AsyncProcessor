namespace AsyncProcessor.Example.Models
{
    public class Customer
    {
        private static Random _Random;

        public string Id { get; set; }
        public string Name { get; set; }
        public IDictionary<string, Address> Addresses { get; set; }

        public static Customer CreateCustomer()
        {
            if (_Random == null)
                _Random = new Random();

            return CreateCustomer(_Random.Next());
        }


        public static Customer CreateCustomer(int id)
        {
            IDictionary<string, Address> addresses = new Dictionary<string, Address>();
            addresses.Add("BILL", CreateBillToAddress());
            addresses.Add("REMIT", CreateRemitToAddress());

            return new Customer()
            {
                Id = id.ToString(),
                Name = String.Format("Customer ({0})", id),
                Addresses = addresses,
            };
        }

        private static Address CreateBillToAddress()
        {
            return new Address()
            {
                Id = _Random.Next().ToString(),
                Line1 = "123 Bill Me St.",
                Line2 = "Attn: Billing Dept",
                City = "Cincinnati",
                State = "OH",
                PostalCode = "45202",
            };
        }

        private static Address CreateRemitToAddress()
        {
            return new Address()
            {
                Id = _Random.Next().ToString(),
                Line1 = "987 Remit To Ln.",
                Line2 = "Attn: Want to get paid Dept",
                City = "Milford",
                State = "OH",
                PostalCode = "45101",
            };
        }
    }
}