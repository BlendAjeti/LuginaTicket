using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Data;
using LuginaTicket.Models;
using LuginaTicket.Services;
using LuginaTicket.ViewModels;

namespace LuginaTicket.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IActionLogService _actionLogService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IActionLogService actionLogService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _actionLogService = actionLogService;
    }

    // GET: Admin/Users
    public async Task<IActionResult> Index(string? search, string? role, int page = 1, int pageSize = 10)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.UserName!.Contains(search) || 
                                    u.Email!.Contains(search) ||
                                    u.FirstName!.Contains(search) ||
                                    u.LastName!.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userViewModels = new List<UserViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            });
        }

        if (!string.IsNullOrEmpty(role))
        {
            userViewModels = userViewModels.Where(u => u.Roles.Contains(role)).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "User", null, "Viewed users list");
        }

        return View(userViewModels);
    }

    // GET: Admin/Users/Details/5
    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var viewModel = new UserViewModel
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt
        };

        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            await _actionLogService.LogActionAsync(userId, "Read", "User", null, $"Viewed user details: {user.UserName}");
        }

        return View(viewModel);
    }

    // GET: Admin/Users/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                
                var userId = _userManager.GetUserId(User);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Create", "User", null, $"Created user: {user.UserName}");
                }

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    // GET: Admin/Users/Edit/5
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? "User"
        };

        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(model);
    }

    // POST: Admin/Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserName = model.Email;
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);

                var userId = _userManager.GetUserId(User);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Update", "User", null, $"Updated user: {user.UserName}");
                }

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return View(model);
    }

    // GET: Admin/Users/Delete/5
    public async Task<IActionResult> Delete(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var viewModel = new UserViewModel
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            Roles = roles.ToList()
        };

        return View(viewModel);
    }

    // POST: Admin/Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            var userName = user.UserName;
            var result = await _userManager.DeleteAsync(user);
            
            if (result.Succeeded)
            {
                var userId = _userManager.GetUserId(User);
                if (userId != null)
                {
                    await _actionLogService.LogActionAsync(userId, "Delete", "User", null, $"Deleted user: {userName}");
                }
            }
        }

        return RedirectToAction(nameof(Index));
    }
}

