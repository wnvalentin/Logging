namespace Microsoft.Framework.Notify.Test
{
    public interface IPerson
    {
        string FirstName { get; }
        string LastName { get; }
        IAddress Address { get; }
    }

    public interface IAddress
    {
        string City { get; }
        string State { get; }
        int Zip { get; }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public Address Address { get; set; }
    }
    public class Address
    {
        public string City { get; set; }
        public string State { get; set; }
        public int Zip { get; set; }
    }

}