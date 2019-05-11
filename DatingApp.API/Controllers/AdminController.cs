using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers {

    [Route ("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase 
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController (DataContext ctx, UserManager<User> userManager) : base () 
        {
            this._context = ctx ??
                throw new ArgumentNullException (nameof (ctx));
            this._userManager = userManager ??
                throw new ArgumentNullException (nameof (userManager));
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles() 
        {
            // use expression link as query
            var users = await (from user in this._context.Users 
                orderby user.UserName 
                select new {
                    Id = user.Id,
                    Username = user.UserName,
                    Roles = (from userRole in user.UserRoles 
                                join role in this._context.Roles 
                                on userRole.RoleId equals role.Id 
                                select role.Name).ToList ()
                }).ToListAsync();

            return Ok(users);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{username}")]
        public async Task<IActionResult> EditRoles (string username, RoleEditDto roleEditDto) 
        {
            var user = await this._userManager.FindByNameAsync (username);

            var userRoles = await this._userManager.GetRolesAsync (user);

            var selectedRoles = roleEditDto.RoleNames;
            selectedRoles = selectedRoles ?? new string[] { };

            var result = await this._userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded)
                return BadRequest ("Failed to Add roles for user " + username);

            result = await this._userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded)
                return BadRequest ("Failed to remove roles for user " + username);

            return Ok(await this._userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratorPhotoRole")]
        [HttpGet("photosForModeration")]
        public IActionResult GetPhotosForModeration() 
        {
            return Ok("admins or moderators can see this");
        }

    }
}