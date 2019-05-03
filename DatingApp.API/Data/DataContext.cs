using DatingApp.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DatingApp.API.Data
{
    /// <summary>
    /// Specify what kind of user we are dealing with
    /// </summary>
    /// <typeparam name="int">type of Key for user</typeparam>
    public class DataContext : IdentityDbContext<User, Role, int, 
        IdentityUserClaim<int>, 
        UserRole, 
        IdentityUserLogin<int>, 
        IdentityRoleClaim<int>,
        IdentityUserToken<int>>
    {
        public DataContext(DbContextOptions<DataContext>  options) : base (options) {}

        #region newregion 

        public DbSet<Value> Values { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }

        #endregion


        protected override void OnModelCreating(ModelBuilder builder)
        {
            // support EF default behaviour for entities
            base.OnModelCreating(builder);

            #region Roles

            builder.Entity<UserRole>(userRole => {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                        .WithMany(ur => ur.UserRoles)
                        .HasForeignKey(ur =>ur.RoleId)
                        .IsRequired();
           
                userRole.HasOne(ur => ur.User)
                        .WithMany(ur => ur.UserRoles)
                        .HasForeignKey(ur =>ur.UserId)
                        .IsRequired();
            });

            #endregion

            // my custom settings / relationships
            builder.Entity<Like>()
                .HasKey(k => new {k.LikerId, k.LikeeId});

            builder.Entity<Like>()
                .HasOne(u => u.Likee)
                .WithMany(u => u.Likers)
                .HasForeignKey(u => u.LikeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Like>()
                .HasOne(u => u.Liker)
                .WithMany(u => u.Likees)
                .HasForeignKey(u => u.LikerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<Message>()
                .HasOne(u => u.Sender)
                .WithMany(m => m.MessagesSent)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(u => u.Recipient)
                .WithMany(m => m.MessagesReceived)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}