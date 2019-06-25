using Accelerant.Core.Exceptions;
using Accelerant.DataLayer.DataCollectors;
using Accelerant.DataLayer.DataProviders;
using Accelerant.DataLayer.DataProviders.Mongo;
using Accelerant.DataTransfer.Models;
using System;
using System.Collections.Generic;

namespace Accelerant.Services.Mongo
{
    public interface IUsersService
        : IService<User, User, Guid>
    {
        User Authenticate(string Username, string Password);
        User GetByName(string Username);
    }
    public class UsersService
        : IUsersService
    {
        private IDataProvider<DataLayer.Models.User, Guid> dataProvider;
        private IDataCollector<DataLayer.Models.User, DataLayer.Models.User, DataLayer.Models.User> dataCollector;

        public UsersService(IDataProvider<DataLayer.Models.User, Guid> dataProvider, IDataCollector<DataLayer.Models.User, DataLayer.Models.User, DataLayer.Models.User> dataCollector)
        {
            this.dataCollector = dataCollector;
            this.dataProvider = dataProvider;
        }

        public User GetByName(string Username)
        {
            var userModel = ((UserProvider)dataProvider).GetByName(Username);
            var userDto = new DataTransfer.Models.User
            {
                Id = userModel.Id,
                Name = userModel.Name,
                Password = null
            };
            return userDto;
        }

        public User Add(User userToAdd)
        {
            var user = ((UserProvider)dataProvider).GetByName(userToAdd.Name);
            if (user != null)
                throw new UserAlreadyExistsException($"Username {userToAdd.Name} already exists.");

            var userModel = new DataLayer.Models.User
            {
                Id = Guid.NewGuid(),
                Name = userToAdd.Name,
                LastActiveTime = null,
                LastLoginTime = null,
                PasswordHash = null,
                PasswordHashSalt = null
            };

            byte[] pwdHash, pwdHashSalt;
            CreatePasswordHash(userToAdd.Password, out pwdHash, out pwdHashSalt);
            userModel.PasswordHash = pwdHash;
            userModel.PasswordHashSalt = pwdHashSalt;

            var addedUser = dataCollector.Add(userModel);

            var userDto = new User
            {
                Id = addedUser.Id,
                Name = addedUser.Name,
                Password = null
            };

            return userDto;
        }

        public User Authenticate(string Username, string Password)
        {
            var user = ((UserProvider)dataProvider).GetByName(Username);
            if (user == null)
                throw new InvalidAuthenticationDataException("Invalid username or password");
            if (!ValidatePasswordHash(Password, user.PasswordHash, user.PasswordHashSalt))
                return null;
            return new DataTransfer.Models.User
            {
                Id = user.Id,
                Name = user.Name
            };
            
        }

        public User Get(Guid id)
        {
            var userModel = dataProvider.Get(id);
            var userDto = new DataTransfer.Models.User
            {
                Id = userModel.Id,
                Name = userModel.Name,
                Password = null
            };
            return userDto;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool ValidatePasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }

        public IEnumerable<User> GetMany(IEnumerable<Guid> Ids)
        {
            throw new NotImplementedException();
        }

        public User Update(User item)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<User> GetAllForUser(Guid UserId)
        {
            throw new NotImplementedException();
        }
    }
}
