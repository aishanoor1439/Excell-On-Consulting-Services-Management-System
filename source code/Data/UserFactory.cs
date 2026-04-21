using System;

namespace ExcellOnServices.Data
{
    // Interface jo base functionalities define karegi
    public interface IUser
    {
        string GetRoleName();
        // Aap yahan mazeed functions add kar sakte hain jo roles ke hisab se badlein
    }

    // Concrete Classes: Har role ki apni class
    public class AdminUser : IUser
    {
        public string GetRoleName() => "Administrator";
    }

    public class ConsultantUser : IUser
    {
        public string GetRoleName() => "Consultant";
    }

    public class ClientUser : IUser
    {
        public string GetRoleName() => "Client";
    }

    // The Factory: Jo decide karega konsa object banana hai
    public class UserFactory
    {
        public static IUser CreateUser(string roleFromDb)
        {
            if (string.IsNullOrEmpty(roleFromDb)) return null;

            switch (roleFromDb)
            {
                case "Admin":
                    return new AdminUser();
                case "Consultant":
                    return new ConsultantUser();
                case "Client":
                    return new ClientUser();
                default:
                    throw new Exception("Unknown Role provided from Database.");
            }
        }
    }
}