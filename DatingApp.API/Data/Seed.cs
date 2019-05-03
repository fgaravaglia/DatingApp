using System;
using System.Collections.Generic;
using System.Linq;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace DatingApp.API.Data
{
    public class Seed
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public Seed(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            this._userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this._roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public void SeedUsers()
        {
            // do it only if there are no users stored in
            if (!this._userManager.Users.Any())
            {
                #region Roles 

                // create roles
                var roles = new List<Role>()
                {
                    new Role(){ Name = "Member"},
                    new Role(){ Name = "Admin"},
                    new Role(){ Name = "Moderator"},
                    new Role(){ Name = "VIP"}
                };

                foreach (var role in roles)
                {
                    // we can use a week passowrd only if startup class has been configured as well
                    _roleManager.CreateAsync(role).Wait();
                }

                #endregion

                var userData = System.IO.File.ReadAllText("Data/UserSeedDataIdentity.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);
                foreach (var user in users)
                {
                    // we can use a week passowrd only if startup class has been configured as well
                    _userManager.CreateAsync(user, "password").Wait();
                    _userManager.AddToRoleAsync(user, "Member").Wait();
                }

                // create admin user
                var adminUser = new User()
                {
                    UserName = "Admin"
                };
                IdentityResult result = _userManager.CreateAsync(adminUser, "password").Result;
                if(result.Succeeded)
                {
                    var admin = this._userManager.FindByNameAsync("Admin").Result;
                    _userManager.AddToRolesAsync(admin, new []{ "Admin", "Moderator"}).Wait();
                }
            }
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }
}